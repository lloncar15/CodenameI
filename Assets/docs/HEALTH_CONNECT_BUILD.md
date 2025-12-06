# Health Connect Integration - Build Instructions

This guide explains how to build and integrate the Android Health Connect plugin with Unity.

## Overview

Health Connect is Google's unified health data API that allows apps to read fitness data (like steps) from various sources including:

- **Garmin Connect** → syncs to Health Connect
- **Samsung Health** → syncs to Health Connect  
- **Fitbit** → syncs to Health Connect
- **Google Fit** → syncs to Health Connect
- **Other fitness apps** → sync to Health Connect

Your Walk Game app can then read this aggregated step data from Health Connect, giving you background step counting without needing your app to run constantly.

## Architecture

```
┌─────────────────┐     ┌─────────────────┐     ┌─────────────────┐
│  Garmin Watch   │────▶│ Health Connect  │◀────│  Samsung Health │
└─────────────────┘     │   (Android OS)  │     └─────────────────┘
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
Plugins/Android/
├── AndroidManifest.xml           # Permissions & app config
├── mainTemplate.gradle           # Dependencies template
├── baseProjectTemplate.gradle    # Gradle plugin config
├── gradleTemplate.properties     # Gradle properties
└── HealthConnectPlugin/          # Native Android plugin source (Java)
    ├── build.gradle
    ├── settings.gradle
    ├── proguard-rules.pro
    └── src/main/
        ├── AndroidManifest.xml
        └── java/.../HealthConnectBridge.java

Scripts/StepTracking/
├── HealthConnectProvider.cs      # Unity C# wrapper
└── IStepProvider.cs              # Interface (already created)
```

---

## Option 1: Use Pre-built Plugin (Recommended)

If we provide a pre-built AAR file:

### Step 1: Copy Files to Unity

1. Copy `HealthConnectPlugin.aar` to `Assets/Plugins/Android/`
2. Copy `AndroidManifest.xml` to `Assets/Plugins/Android/`
3. Copy the Gradle templates to `Assets/Plugins/Android/`
4. Copy `HealthConnectProvider.cs` to your Scripts folder

### Step 2: Configure Unity Project

1. **Open Project Settings**: Edit → Project Settings → Player → Android

2. **Set Minimum API Level**:
   - Other Settings → Minimum API Level → **Android 9.0 (API 28)**
   
3. **Enable Custom Gradle Templates**:
   - Publishing Settings → Check all:
     - ☑ Custom Main Gradle Template
     - ☑ Custom Base Gradle Template  
     - ☑ Custom Gradle Properties Template
     - ☑ Custom Main Manifest

4. **Copy Template Contents**:
   - Copy content from our template files to the Unity-generated files in `Assets/Plugins/Android/`

### Step 3: Build and Test

1. Build APK: File → Build Settings → Build
2. Install on Android device
3. Open Health Connect app on device
4. Grant permissions when prompted
5. Test step reading in your app

---

## Option 2: Build Plugin From Source

If you need to modify the native code or the pre-built AAR isn't working:

### Prerequisites

- Android Studio (latest version)
- Android SDK 34+
- Java 8+ (included with Android Studio)
- Gradle 8.4+

### Step 1: Get Unity Classes JAR

The plugin needs Unity's classes to send messages back to Unity.

1. Build any Android project in Unity once
2. Find `unity-classes.jar` in your project's Temp folder:
   ```
   [Project]/Temp/gradleOut/unityLibrary/libs/unity-classes.jar
   ```
3. Copy it to:
   ```
   Plugins/Android/HealthConnectPlugin/libs/unity-classes.jar
   ```

### Step 2: Open in Android Studio

1. Open Android Studio
2. File → Open → Select `Plugins/Android/HealthConnectPlugin/`
3. Wait for Gradle sync to complete

### Step 3: Build AAR

```bash
# From command line:
cd Plugins/Android/HealthConnectPlugin
./gradlew assembleRelease

# AAR will be in:
# build/outputs/aar/HealthConnectPlugin-release.aar
```

Or in Android Studio:
1. Build → Make Project
2. Find AAR in `build/outputs/aar/`

### Step 4: Copy AAR to Unity

```bash
cp build/outputs/aar/HealthConnectPlugin-release.aar ../HealthConnectPlugin.aar
```

Then continue with Option 1, Step 2.

---

## Gradle Version Compatibility

### The Problem

Health Connect SDK 1.1.0+ requires Android Gradle Plugin 8.9.1+, but Unity 2022.3 ships with AGP 8.7.2.

### Solutions

#### Solution A: Use Older Health Connect Version

We use `connect-client:1.1.0-alpha07` which works with AGP 8.7.2. This is the approach in our templates.

#### Solution B: Upgrade Gradle in Unity

If you need newer Health Connect features:

1. **Download Gradle 8.11.1** from https://gradle.org/releases/
2. **Extract** to a folder (e.g., `C:/Gradle/gradle-8.11.1`)
3. **Configure Unity**:
   - Edit → Preferences → External Tools
   - Uncheck "Gradle installed with Unity"
   - Browse to your Gradle folder
4. **Update baseProjectTemplate.gradle**:
   ```gradle
   classpath 'com.android.tools.build:gradle:8.9.3'
   ```

---

## Usage in Unity

### Basic Setup

```csharp
using WalkGame.StepTracking;
using UnityEngine;

public class HealthConnectDemo : MonoBehaviour
{
    void Start()
    {
        var provider = HealthConnectProvider.Instance;
        
        // Subscribe to events
        provider.OnInitialized += OnInitialized;
        provider.OnPermissionsGranted += OnPermissions;
        provider.OnStepsQueried += OnSteps;
        provider.OnError += OnError;
        
        // Check availability
        var availability = provider.CheckAvailability();
        Debug.Log($"Health Connect: {availability}");
        
        if (availability == HealthConnectAvailability.Available)
        {
            provider.Initialize();
        }
        else if (availability == HealthConnectAvailability.NeedsUpdate)
        {
            // Prompt user to install Health Connect
            provider.OpenPlayStore();
        }
    }
    
    void OnInitialized(bool success)
    {
        if (success)
        {
            // Request permissions
            HealthConnectProvider.Instance.RequestPermissions();
        }
    }
    
    void OnPermissions(bool granted)
    {
        if (granted)
        {
            // Query today's steps
            HealthConnectProvider.Instance.QueryStepsToday();
        }
    }
    
    void OnSteps(HealthConnectStepResult result)
    {
        if (result.success)
        {
            Debug.Log($"Steps: {result.steps}");
        }
    }
    
    void OnError(string code, string message)
    {
        Debug.LogError($"Health Connect Error [{code}]: {message}");
    }
}
```

### Using with StepDataManager

```csharp
// In StepDataManager, you can add Health Connect as a provider:

void InitializeHealthConnect()
{
    if (Application.platform == RuntimePlatform.Android)
    {
        var hcProvider = HealthConnectProvider.Instance;
        
        if (hcProvider.CheckAvailability() == HealthConnectAvailability.Available)
        {
            hcProvider.OnStepsQueried += OnHealthConnectSteps;
            hcProvider.Initialize();
        }
    }
}

void OnHealthConnectSteps(HealthConnectStepResult result)
{
    if (result.success)
    {
        // Record steps from Health Connect
        AddStepsManually((int)result.steps);
    }
}
```

---

## Testing

### On Device

1. Install Walk Game APK
2. Install Health Connect from Play Store (if not present)
3. Open Health Connect → App permissions
4. Verify Walk Game appears and has step permission
5. Use another app (like Google Fit) to log steps
6. Query steps from Walk Game

### In Unity Editor

Health Connect is Android-only. In Editor:

```csharp
#if UNITY_EDITOR
// Simulate Health Connect
Debug.Log("Health Connect not available in Editor - using mock data");
OnSteps(new HealthConnectStepResult { success = true, steps = 5000 });
#endif
```

---

## Troubleshooting

### "Health Connect not available"

- Device needs Android 9+
- Health Connect app must be installed
- Some regions may not have Health Connect

### "NoClassDefFoundError" at runtime

- Dependencies not properly included
- Check mainTemplate.gradle has Health Connect dependency
- Rebuild with Clean Build

### "Permission denied"

- User must grant permission in Health Connect app
- Go to Health Connect → App permissions → Walk Game

### Build fails with "Gradle version" error

- See Gradle Version Compatibility section above
- Try using older Health Connect SDK version

### No steps returned

- Ensure other apps are writing to Health Connect
- Check the time range being queried
- Verify permissions are granted

---

## Resources

- [Health Connect Documentation](https://developer.android.com/health-and-fitness/guides/health-connect)
- [Health Connect SDK Releases](https://developer.android.com/jetpack/androidx/releases/health-connect)
- [Unity Android Plugins](https://docs.unity3d.com/Manual/AndroidNativePlugins.html)
- [GameCI for CI/CD](https://game.ci)
