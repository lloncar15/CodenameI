#if UNITY_IOS
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.iOS.Xcode;
using System.IO;
using System.Linq;

namespace StepTracking.Editor {
    /// <summary>
    /// Post-processor that adds required Info.plist entries for step tracking on iOS.
    /// This automatically:
    /// - Adds HealthKit capability
    /// - Adds HealthKit framework
    /// - Adds required Info.plist usage descriptions
    /// 
    /// This runs automatically when you build for iOS.
    /// </summary>
    public static class StepTrackingIOSPostProcessor {
        // The message shown to users when requesting motion permission
        private const string MOTION_USAGE_DESCRIPTION = 
            "This app uses motion data to count your steps and reward your walking activity."; 
        
        // The message shown to users when requesting HealthKit read permission
        private const string HEALTH_SHARE_USAGE_DESCRIPTION = 
            "This app reads your step count from Apple Health to track your walking progress and award points.";
        
        // The message shown if the app writes to HealthKit (not used in this app, but required key)
        private const string HEALTH_UPDATE_USAGE_DESCRIPTION = 
            "This app can save your walking activity to Apple Health.";
    
        /// <summary>
        /// Automatically changes the HealthKit capability and Info plist.
        /// </summary>
        [PostProcessBuild(100)]
        public static void OnPostProcessBuild(BuildTarget target, string path) {
            if (target != BuildTarget.iOS)
                return;
            
            UnityEngine.Debug.Log("[StepTrackingIOSPostProcessor] Starting iOS post-processing...");
            
            ModifyXcodeProject(path);
            ModifyInfoPlist(path);
            AddEntitlements(path);
        
            UnityEngine.Debug.Log("[StepTrackingIOSPostProcessor] iOS post-processing complete");
        }
        
        /// <summary>
        /// Adds the HealthKit framework and capability to the project
        /// NOTE: this adds the capability, but it may still need to be enabled in Xcode for code signing purposes
        /// </summary>
        private static void ModifyXcodeProject(string path) {
            string projectPath = PBXProject.GetPBXProjectPath(path);
            PBXProject project = new();
            project.ReadFromFile(projectPath);
            
            string mainTargetGuid = project.GetUnityMainTargetGuid();
            string frameworkTargetGuid = project.GetUnityFrameworkTargetGuid();
            
            project.AddFrameworkToProject(mainTargetGuid, "HealthKit.framework", false);
            UnityEngine.Debug.Log("[StepTrackingIOSPostProcessor] Added HealthKit.framework");
            
            project.AddCapability(mainTargetGuid, PBXCapabilityType.HealthKit);
            UnityEngine.Debug.Log("[StepTrackingIOSPostProcessor] Added HealthKit capability");
            
            project.WriteToFile(projectPath);
        }
        
        /// <summary>
        /// Adds the following into the Info.plist file:
        /// - motion usage description (required for pedometer/CMPedometer access)
        /// - HealthKit share usage description (required for reading HealthKit data)
        /// - HealthKit update usage description (required even if not writing)
        /// - UIRequiredDeviceCapabilities for HealthKit (optional but recommended)
        /// </summary>
        private static void ModifyInfoPlist(string path) {
            string plistPath = Path.Combine(path, "Info.plist");
            PlistDocument plist = new();
            plist.ReadFromFile(plistPath);
            
            TrySettingKeyValuePair(plist, "NSMotionUsageDescription", MOTION_USAGE_DESCRIPTION);
            TrySettingKeyValuePair(plist, "NSHealthShareUsageDescription", HEALTH_SHARE_USAGE_DESCRIPTION);
            TrySettingKeyValuePair(plist, "NSHealthUpdateUsageDescription", HEALTH_UPDATE_USAGE_DESCRIPTION);

            PlistElementArray deviceCapabilities = plist.root.values.ContainsKey("UIRequiredDeviceCapabilities") ? 
                plist.root["UIRequiredDeviceCapabilities"].AsArray() : 
                plist.root.CreateArray("UIRequiredDeviceCapabilities");
            
            bool hasHealthKit = deviceCapabilities.values.Any(element => element.AsString() == "healthkit");

            if (!hasHealthKit) {
                deviceCapabilities.AddString("healthkit");
                UnityEngine.Debug.Log("[StepTrackingIOSPostProcessor] Added healthkit to UIRequiredDeviceCapabilities");
            }
            
            plist.WriteToFile(plistPath);
        }

        /// <summary>
        /// Tries setting the value for the key in a given PlistDocument
        /// </summary>
        private static void TrySettingKeyValuePair(PlistDocument doc, string key, string value) {
            if (doc.root.values.ContainsKey(key)) 
                return;
            
            doc.root.SetString(key, value);
            UnityEngine.Debug.Log($"[StepTrackingIOSPostProcessor] Added {key}");
        }
        
        /// <summary>
        /// Adds HealthKit entitlement and HealthKit access array which specifies what health data types we access.
        /// We're accessing health records (step count falls under this)
        /// and empty array means we just need basic HealthKit access
        /// </summary>
        private static void AddEntitlements(string path) {
            string entitlementsPath = Path.Combine(path, "Unity-iPhone", "Unity-iPhone.entitlements");
            
            string entitlementsDir = Path.GetDirectoryName(entitlementsPath);
            if (!Directory.Exists(entitlementsDir)) {
                entitlementsPath = Path.Combine(path, "Unity-iPhone.entitlements");
            }
            
            PlistDocument entitlements = new();
            
            if (File.Exists(entitlementsPath)) {
                entitlements.ReadFromFile(entitlementsPath);
            }
            
            if (!entitlements.root.values.ContainsKey("com.apple.developer.healthkit")) {
                entitlements.root.SetBoolean("com.apple.developer.healthkit", true);
                UnityEngine.Debug.Log("[StepTrackingIOSPostProcessor] Added HealthKit entitlement");
            }
            
            if (!entitlements.root.values.ContainsKey("com.apple.developer.healthkit.access")) {
                PlistElementArray accessArray = entitlements.root.CreateArray("com.apple.developer.healthkit.access");
                UnityEngine.Debug.Log("[StepTrackingIOSPostProcessor] Added HealthKit access entitlement");
            }
            
            entitlements.WriteToFile(entitlementsPath);
            
            string projectPath = PBXProject.GetPBXProjectPath(path);
            PBXProject project = new();
            project.ReadFromFile(projectPath);
            
            string mainTargetGuid = project.GetUnityMainTargetGuid();
            
            project.SetBuildProperty(mainTargetGuid, "CODE_SIGN_ENTITLEMENTS", 
                entitlementsPath.Replace(path + "/", ""));
            
            project.WriteToFile(projectPath);
        }
    }
}
#endif
