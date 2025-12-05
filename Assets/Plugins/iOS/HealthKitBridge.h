/*
 * HealthKitBridge.h
 * Unity bridge for iOS HealthKit API
 *
 * This header declares the C functions that Unity can call via DllImport.
 */

#ifndef HealthKitBridge_h
#define HealthKitBridge_h

#import <Foundation/Foundation.h>

#ifdef __cplusplus
extern "C" {
#endif

// ============================================================
// Availability & Authorization
// ============================================================

/**
 * Check if HealthKit is available on this device
 * @return 1 if available, 0 if not
 */
int HealthKit_IsAvailable(void);

/**
 * Check if step count permission has been granted
 * @return 1 if authorized, 0 if not, -1 if not determined
 */
int HealthKit_IsAuthorized(void);

/**
 * Request authorization to read step count data
 * Result is sent via UnitySendMessage to "HealthKitReceiver" with method "OnAuthorizationResult"
 */
void HealthKit_RequestAuthorization(void);

// ============================================================
// Step Queries
// ============================================================

/**
 * Query steps since a given timestamp
 * @param timestampMillis Milliseconds since epoch (UTC)
 * Result is sent via UnitySendMessage to "HealthKitReceiver" with method "OnStepsReceived"
 */
void HealthKit_GetStepsSince(long long timestampMillis);

/**
 * Query steps for a date range
 * @param startMillis Start time in milliseconds since epoch (UTC)
 * @param endMillis End time in milliseconds since epoch (UTC)
 * Result is sent via UnitySendMessage to "HealthKitReceiver" with method "OnStepsReceived"
 */
void HealthKit_GetStepsForRange(long long startMillis, long long endMillis);

/**
 * Query steps for today
 * Result is sent via UnitySendMessage to "HealthKitReceiver" with method "OnStepsReceived"
 */
void HealthKit_GetStepsToday(void);

// ============================================================
// Settings
// ============================================================

/**
 * Open the iOS Health app
 */
void HealthKit_OpenHealthApp(void);

#ifdef __cplusplus
}
#endif

#endif /* HealthKitBridge_h */
