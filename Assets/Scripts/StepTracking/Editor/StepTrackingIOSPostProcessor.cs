#if UNITY_IOS
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.iOS.Xcode;
using System.IO;

namespace StepTracking.Editor {
    /// <summary>
    /// Post-processor that adds required Info.plist entries for step tracking on iOS.
    /// This runs automatically when you build for iOS.
    /// </summary>
    public static class StepTrackingIOSPostProcessor {
        // The message shown to users when requesting motion permission
        private const string MOTION_USAGE_DESCRIPTION = 
            "This app uses motion data to count your steps and reward your walking activity.";
    
        /// <summary>
        /// Adds the motion usage description that is needed for the pedometer access
        /// </summary>
        [PostProcessBuild(100)]
        public static void OnPostProcessBuild(BuildTarget target, string path) {
            if (target != BuildTarget.iOS)
                return;
            
            string plistPath = Path.Combine(path, "Info.plist");
            PlistDocument plist = new PlistDocument();
            plist.ReadFromFile(plistPath);
            
            if (!plist.root.values.ContainsKey("NSMotionUsageDescription")) {
                plist.root.SetString("NSMotionUsageDescription", MOTION_USAGE_DESCRIPTION);
                UnityEngine.Debug.Log("[StepTrackingIOSPostProcessor] Added NSMotionUsageDescription to Info.plist");
            }
            
            plist.WriteToFile(plistPath);
        
            UnityEngine.Debug.Log("[StepTrackingIOSPostProcessor] iOS post-processing complete");
        }
    }
}

#endif