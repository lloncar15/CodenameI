/*
 * HealthKitBridge.m
 * Unity bridge for iOS HealthKit API
 *
 * This file implements the native iOS side of the HealthKit integration.
 * It communicates with Unity via UnitySendMessage.
 */

#import "HealthKitBridge.h"
#import <HealthKit/HealthKit.h>
#import <UIKit/UIKit.h>

// Unity function to send messages back to C#
extern void UnitySendMessage(const char* obj, const char* method, const char* msg);

// ============================================================
// Private Interface
// ============================================================

@interface HealthKitManager : NSObject

@property (nonatomic, strong) HKHealthStore *healthStore;
@property (nonatomic, strong) NSSet<HKObjectType *> *typesToRead;

+ (instancetype)sharedInstance;

- (BOOL)isHealthKitAvailable;
- (HKAuthorizationStatus)authorizationStatus;
- (void)requestAuthorizationWithCompletion:(void(^)(BOOL success, NSError *error))completion;
- (void)queryStepsFromDate:(NSDate *)startDate toDate:(NSDate *)endDate completion:(void(^)(double steps, NSError *error))completion;

@end

// ============================================================
// Implementation
// ============================================================

@implementation HealthKitManager

+ (instancetype)sharedInstance {
    static HealthKitManager *instance = nil;
    static dispatch_once_t onceToken;
    dispatch_once(&onceToken, ^{
        instance = [[HealthKitManager alloc] init];
    });
    return instance;
}

- (instancetype)init {
    self = [super init];
    if (self) {
        if ([HKHealthStore isHealthDataAvailable]) {
            _healthStore = [[HKHealthStore alloc] init];
            
            // Define the types we want to read
            HKQuantityType *stepCountType = [HKQuantityType quantityTypeForIdentifier:HKQuantityTypeIdentifierStepCount];
            _typesToRead = [NSSet setWithObject:stepCountType];
        }
    }
    return self;
}

- (BOOL)isHealthKitAvailable {
    return [HKHealthStore isHealthDataAvailable];
}

- (HKAuthorizationStatus)authorizationStatus {
    if (!self.healthStore) {
        return HKAuthorizationStatusNotDetermined;
    }
    
    HKQuantityType *stepCountType = [HKQuantityType quantityTypeForIdentifier:HKQuantityTypeIdentifierStepCount];
    return [self.healthStore authorizationStatusForType:stepCountType];
}

- (void)requestAuthorizationWithCompletion:(void(^)(BOOL success, NSError *error))completion {
    if (!self.healthStore) {
        NSError *error = [NSError errorWithDomain:@"HealthKitBridge"
                                             code:-1
                                         userInfo:@{NSLocalizedDescriptionKey: @"HealthKit not available"}];
        if (completion) {
            completion(NO, error);
        }
        return;
    }
    
    [self.healthStore requestAuthorizationToShareTypes:nil
                                             readTypes:self.typesToRead
                                            completion:^(BOOL success, NSError * _Nullable error) {
        dispatch_async(dispatch_get_main_queue(), ^{
            if (completion) {
                completion(success, error);
            }
        });
    }];
}

- (void)queryStepsFromDate:(NSDate *)startDate toDate:(NSDate *)endDate completion:(void(^)(double steps, NSError *error))completion {
    if (!self.healthStore) {
        NSError *error = [NSError errorWithDomain:@"HealthKitBridge"
                                             code:-1
                                         userInfo:@{NSLocalizedDescriptionKey: @"HealthKit not available"}];
        if (completion) {
            completion(0, error);
        }
        return;
    }
    
    HKQuantityType *stepCountType = [HKQuantityType quantityTypeForIdentifier:HKQuantityTypeIdentifierStepCount];
    
    // Use HKStatisticsQuery to get aggregated step count (avoids double-counting from multiple sources)
    NSPredicate *predicate = [HKQuery predicateForSamplesWithStartDate:startDate
                                                               endDate:endDate
                                                               options:HKQueryOptionStrictStartDate];
    
    HKStatisticsQuery *query = [[HKStatisticsQuery alloc]
                                initWithQuantityType:stepCountType
                                quantitySamplePredicate:predicate
                                options:HKStatisticsOptionCumulativeSum
                                completionHandler:^(HKStatisticsQuery * _Nonnull query,
                                                   HKStatistics * _Nullable result,
                                                   NSError * _Nullable error) {
        dispatch_async(dispatch_get_main_queue(), ^{
            if (error) {
                if (completion) {
                    completion(0, error);
                }
                return;
            }
            
            HKQuantity *sumQuantity = [result sumQuantity];
            double steps = 0;
            
            if (sumQuantity) {
                steps = [sumQuantity doubleValueForUnit:[HKUnit countUnit]];
            }
            
            if (completion) {
                completion(steps, nil);
            }
        });
    }];
    
    [self.healthStore executeQuery:query];
}

@end

// ============================================================
// Helper Functions
// ============================================================

static void SendMessageToUnity(const char *method, NSString *message) {
    const char *gameObject = "HealthKitReceiver";
    const char *msg = [message UTF8String];
    UnitySendMessage(gameObject, method, msg);
}

static void SendSuccessToUnity(double steps, long long startMillis, long long endMillis) {
    NSString *json = [NSString stringWithFormat:
                      @"{\"success\":true,\"steps\":%lld,\"startTime\":%lld,\"endTime\":%lld,\"source\":\"HealthKit\"}",
                      (long long)steps, startMillis, endMillis];
    SendMessageToUnity("OnStepsReceived", json);
}

static void SendErrorToUnity(NSString *errorCode, NSString *errorMessage) {
    // Escape quotes in error message
    NSString *escapedMessage = [errorMessage stringByReplacingOccurrencesOfString:@"\"" withString:@"\\\""];
    
    NSString *json = [NSString stringWithFormat:
                      @"{\"success\":false,\"errorCode\":\"%@\",\"errorMessage\":\"%@\"}",
                      errorCode, escapedMessage];
    SendMessageToUnity("OnHealthKitError", json);
}

static NSDate* DateFromMillis(long long millis) {
    return [NSDate dateWithTimeIntervalSince1970:(millis / 1000.0)];
}

static long long MillisFromDate(NSDate *date) {
    return (long long)([date timeIntervalSince1970] * 1000.0);
}

// ============================================================
// C Interface Implementation
// ============================================================

int HealthKit_IsAvailable(void) {
    return [[HealthKitManager sharedInstance] isHealthKitAvailable] ? 1 : 0;
}

int HealthKit_IsAuthorized(void) {
    HKAuthorizationStatus status = [[HealthKitManager sharedInstance] authorizationStatus];
    
    switch (status) {
        case HKAuthorizationStatusSharingAuthorized:
            return 1;
        case HKAuthorizationStatusSharingDenied:
            return 0;
        case HKAuthorizationStatusNotDetermined:
        default:
            return -1;
    }
}

void HealthKit_RequestAuthorization(void) {
    [[HealthKitManager sharedInstance] requestAuthorizationWithCompletion:^(BOOL success, NSError *error) {
        if (error) {
            NSLog(@"[HealthKitBridge] Authorization error: %@", error.localizedDescription);
        }
        
        // Note: 'success' only indicates that the dialog was shown, not that permission was granted
        // We need to check the actual authorization status
        HKAuthorizationStatus status = [[HealthKitManager sharedInstance] authorizationStatus];
        BOOL authorized = (status == HKAuthorizationStatusSharingAuthorized);
        
        NSString *result = authorized ? @"true" : @"false";
        SendMessageToUnity("OnAuthorizationResult", result);
    }];
}

void HealthKit_GetStepsSince(long long timestampMillis) {
    NSDate *startDate = DateFromMillis(timestampMillis);
    NSDate *endDate = [NSDate date];
    
    long long startMillis = timestampMillis;
    long long endMillis = MillisFromDate(endDate);
    
    [[HealthKitManager sharedInstance] queryStepsFromDate:startDate
                                                   toDate:endDate
                                               completion:^(double steps, NSError *error) {
        if (error) {
            NSLog(@"[HealthKitBridge] Query error: %@", error.localizedDescription);
            SendErrorToUnity(@"QueryFailed", error.localizedDescription);
        } else {
            NSLog(@"[HealthKitBridge] Steps since %@: %.0f", startDate, steps);
            SendSuccessToUnity(steps, startMillis, endMillis);
        }
    }];
}

void HealthKit_GetStepsForRange(long long startMillis, long long endMillis) {
    NSDate *startDate = DateFromMillis(startMillis);
    NSDate *endDate = DateFromMillis(endMillis);
    
    [[HealthKitManager sharedInstance] queryStepsFromDate:startDate
                                                   toDate:endDate
                                               completion:^(double steps, NSError *error) {
        if (error) {
            NSLog(@"[HealthKitBridge] Query error: %@", error.localizedDescription);
            SendErrorToUnity(@"QueryFailed", error.localizedDescription);
        } else {
            NSLog(@"[HealthKitBridge] Steps for range: %.0f", steps);
            SendSuccessToUnity(steps, startMillis, endMillis);
        }
    }];
}

void HealthKit_GetStepsToday(void) {
    NSCalendar *calendar = [NSCalendar currentCalendar];
    NSDate *now = [NSDate date];
    NSDate *startOfDay = [calendar startOfDayForDate:now];
    
    long long startMillis = MillisFromDate(startOfDay);
    long long endMillis = MillisFromDate(now);
    
    [[HealthKitManager sharedInstance] queryStepsFromDate:startOfDay
                                                   toDate:now
                                               completion:^(double steps, NSError *error) {
        if (error) {
            NSLog(@"[HealthKitBridge] Query error: %@", error.localizedDescription);
            SendErrorToUnity(@"QueryFailed", error.localizedDescription);
        } else {
            NSLog(@"[HealthKitBridge] Steps today: %.0f", steps);
            SendSuccessToUnity(steps, startMillis, endMillis);
        }
    }];
}

void HealthKit_OpenHealthApp(void) {
    dispatch_async(dispatch_get_main_queue(), ^{
        NSURL *healthURL = [NSURL URLWithString:@"x-apple-health://"];
        
        if ([[UIApplication sharedApplication] canOpenURL:healthURL]) {
            [[UIApplication sharedApplication] openURL:healthURL
                                               options:@{}
                                     completionHandler:nil];
        } else {
            NSLog(@"[HealthKitBridge] Cannot open Health app");
        }
    });
}
