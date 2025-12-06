# HealthKit Integration - iOS Build Instructions

This guide explains how to build and integrate the iOS HealthKit plugin with Unity.

## Overview

HealthKit is Apple's health data framework that allows apps to read fitness data from various sources including:

- **Apple Watch** → syncs steps, workouts to iPhone Health app
- **iPhone Motion Coprocessor** → tracks steps automatically
- **Third-party apps** → Garmin Connect, Strava, etc. can write to Health
- **Manual entries** → Users can manually log data

Your Walk Game app can read this aggregated step data from HealthKit, giving you access to steps counted even when your app wasn't running.

## Architecture

```
┌─────────────────┐     ┌─────────────────┐     ┌─────────────────┐
│  Apple Watch    │────▶│                 │◀────│  Garmin Connect │
│  iPhone M-chip  │────▶│  Apple Health   │◀────│  Strava, etc.   │
└─────────────────┘     │   (HealthKit)   │     └─────────────────┘
                        └────────┬────────┘
                                 │ Query steps
                                 ▼
                        ┌─────────────────┐
                        │  HealthKit      │
                        │  Provider (C#)  │
                        │       ↕         │
                        │  HealthKit      │
                        │  Bridge (Obj-C) │
                        └────────┬────────┘
                                 │
                                 ▼
                        ┌─────────────────┐
                        │   Walk Game     │
                        │   (Unity App)   │
                        └─────────────────┘
```

## Files Included

```
Plugins/iOS/
├── HealthKitBridge.h             # C header for Unity DllImport
└── HealthKitBridge.m             # Objective-C implementation

Scripts/StepTracking/
├── HealthKitProvider.cs          # Unity C# wrapper

Scripts/Editor/
└── StepTrackingIOSPostProcessor.cs  # Auto-configures Xcode project
```

---

## Automatic Setup (Recommended)

The `StepTrackingIOSPostProcessor.cs` script automatically configures your Xcode project when you build. It:

1. ✅ Adds HealthKit framework
2. ✅ Adds HealthKit capability
3. ✅ Creates entitlements file
4. ✅ Adds required Info.plist entries:
   - `NSMotionUsageDescription` (for pedometer)
   - `NSHealthShareUsageDescription` (for reading HealthKit)
   - `NSHealthUpdateUsageDescription` (for writing HealthKit)

### Steps

1. Copy all files to your Unity project
2. Build for iOS: File → Build Settings → iOS → Build
3. Open the generated Xcode project
4. Select your development team for code signing
5. Build and run on device

---

## Manual Setup (If Auto-Setup Fails)

If the post-processor doesn't work (e.g., another script conflicts), follow these manual steps:

### 1. Add HealthKit Framework

1. Open the Xcode project
2. Select the Unity-iPhone project in the navigator
3. Select the Unity-iPhone target
4. Go to "Build Phases" tab
5. Expand "Link Binary With Libraries"
6. Click "+" and add `HealthKit.framework`

### 2. Enable HealthKit Capability

1. Select the Unity-iPhone target
2. Go to "Signing & Capabilities" tab
3. Click "+ Capability"
4. Add "HealthKit"
5. Ensure "Clinical Health Records" is unchecked (we don't need it)

### 3. Add Info.plist Entries

Open `Info.plist` and add:

```xml
<key>NSMotionUsageDescription</key>
<string>This app uses motion data to count your steps and reward your walking activity.</string>

<key>NSHealthShareUsageDescription</key>
<string>This app reads your step count from Apple Health to track your walking progress and award points.</string>

<key>NSHealthUpdateUsageDescription</key>
<string>This app can save your walking activity to Apple Health.</string>
```

### 4. Create Entitlements File

Create `Unity-iPhone.entitlements`:

```xml
<?xml version="1.0" encoding="UTF-8"?>
<!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
<plist version="1.0">
<dict>
    <key>com.apple.developer.healthkit</key>
    <true/>
    <key>com.apple.developer.healthkit.access</key>
    <array/>
</dict>
</plist>
```

---

## Usage in Unity

### Basic Setup

```csharp
using WalkGame.StepTracking;
using UnityEngine;

public class HealthKitDemo : MonoBehaviour
{
    void Start()
    {
        var provider = HealthKitProvider.Instance;
        
        // Check availability
        if (provider.CheckAvailability())
        {
            Debug.Log("HealthKit is available!");
            
            // Subscribe to events
            provider.OnAuthorizationComplete += OnAuth;
            provider.OnStepsQueried += OnSteps;
            provider.OnError += OnError;
            
            // Request permission
            provider.RequestPermissions();
        }
        else
        {
            Debug.Log("HealthKit not available on this device");
        }
    }
    
    void OnAuth(bool authorized)
    {
        if (authorized)
        {
            Debug.Log("HealthKit authorized! Querying steps...");
            HealthKitProvider.Instance.QueryStepsToday();
        }
        else
        {
            Debug.Log("HealthKit permission denied");
        }
    }
    
    void OnSteps(HealthKitStepResult result)
    {
        if (result.success)
        {
            Debug.Log($"Steps: {result.steps}");
        }
    }
    
    void OnError(string code, string message)
    {
        Debug.LogError($"HealthKit Error [{code}]: {message}");
    }
}
```

### Using with StepDataManager

```csharp
void InitializeHealthKit()
{
    if (Application.platform == RuntimePlatform.IPhonePlayer)
    {
        var hkProvider = HealthKitProvider.Instance;
        
        if (hkProvider.CheckAvailability())
        {
            hkProvider.OnStepsQueried += OnHealthKitSteps;
            hkProvider.RequestPermissions();
        }
    }
}

void OnHealthKitSteps(HealthKitStepResult result)
{
    if (result.success)
    {
        StepDataManager.Instance.AddStepsManually((int)result.steps);
    }
}
```

### Query Methods

```csharp
var provider = HealthKitProvider.Instance;

// Query today's steps
provider.QueryStepsToday();

// Query steps since a specific time
DateTime since = DateTime.Now.AddDays(-7);
long sinceMillis = new DateTimeOffset(since).ToUnixTimeMilliseconds();
provider.QueryStepsSince(sinceMillis);

// Query steps for a date range
DateTime start = DateTime.Today;
DateTime end = DateTime.Now;
provider.QueryStepsForRange(start, end);
```

---

## Testing

### On Device

1. Build and run on a physical iPhone
2. Walk around to generate steps
3. Or use another app (like Apple's Health app) to add step data
4. Query steps from your app

### In Simulator

HealthKit works in the iOS Simulator, but:
- No actual step data is generated
- You can manually add test data via the Health app in Simulator
- The Simulator's Health app is at: Features → Health Data

### In Unity Editor

HealthKit is iOS-only. In Editor, use mock data:

```csharp
#if UNITY_EDITOR
void SimulateHealthKitSteps()
{
    var fakeResult = new HealthKitStepResult
    {
        success = true,
        steps = 5000,
        startTime = DateTimeOffset.Now.AddHours(-8).ToUnixTimeMilliseconds(),
        endTime = DateTimeOffset.Now.ToUnixTimeMilliseconds(),
        source = "HealthKit"
    };
    
    OnSteps(fakeResult);
}
#endif
```

---

## Troubleshooting

### "HealthKit not available"

- HealthKit requires iOS 8.0+
- HealthKit is not available on iPad (no step counter hardware)
- Make sure you're testing on a physical iPhone, not Simulator

### "Authorization denied"

- User denied permission
- Go to Settings → Privacy → Health → Your App to re-enable
- Or: Settings → Health → Data Access & Devices → Your App

### Build Error: "HealthKit.framework not found"

- Add HealthKit framework manually in Xcode
- Make sure you have the iOS SDK installed

### Build Error: "Missing entitlements"

- Enable HealthKit capability in Xcode
- Check that entitlements file exists and is referenced in build settings

### Steps Return 0

- Make sure you've walked with your iPhone (or Apple Watch synced)
- Check the Health app directly to verify step data exists
- Ensure the time range for your query includes actual step data

### Permission Dialog Not Showing

- `NSHealthShareUsageDescription` must be in Info.plist
- The dialog only shows once per app install
- To re-test, delete the app and reinstall

---

## Privacy Considerations

- HealthKit data is **highly sensitive**
- Apple reviews HealthKit apps carefully
- You must have a clear privacy policy
- Only request the data types you actually need
- Never share HealthKit data without explicit user consent

---

## App Store Submission

When submitting to the App Store:

1. **Enable HealthKit in App Store Connect**:
   - My Apps → Your App → App Information
   - Check "HealthKit" under App Capabilities

2. **Provide Privacy Policy URL**:
   - Required for any app using HealthKit
   - Must explain how health data is used

3. **Answer App Review Questions**:
   - Apple will ask why you need HealthKit access
   - Be specific: "To count user steps for gamification"

---

## Resources

- [Apple HealthKit Documentation](https://developer.apple.com/documentation/healthkit)
- [HealthKit Best Practices](https://developer.apple.com/videos/play/wwdc2022/10005/)
- [App Store Review Guidelines - HealthKit](https://developer.apple.com/app-store/review/guidelines/#health-and-health-research)
