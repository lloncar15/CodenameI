using System;
using UnityEngine;
using UnityEngine.Android;
using UnityEngine.InputSystem;

namespace GimGim.StepTracking {
    /// <summary>
    /// Step provider using Unity's Input System StepCounter.
    /// Works on iOS and Android devices with pedometer hardware.
    /// 
    /// Requirements:
    /// - Unity Input System package installed
    /// - iOS: NSMotionUsageDescription in Info.plist
    /// - Android: ACTIVITY_RECOGNITION permission in AndroidManifest.xml
    /// </summary>
    public class InputSystemStepProvider : MonoBehaviour, IStepProvider {
        #region Private Fields
        
        private StepCounter _stepCounter;
        /// <summary>
        /// Steps at start of tracking session
        /// </summary>
        private int _baselineSteps = 0;
        /// <summary>
        /// Last read value for delta calculation
        /// </summary>
        private int _lastReadSteps = 0;
        /// <summary>
        /// Steps accumulated this session
        /// </summary>
        private int _sessionSteps = 0;
        private bool _isTracking = false;
        private bool _isAuthorized = false;

        #endregion

        #region Properties and Getters

        public bool IsTracking => _isTracking;

        public int GetSessionSteps() => _sessionSteps;

        #endregion

        #region IStepProvider Implementation

        public StepSource Source => StepSource.InputSystemPedometer;

        public bool IsAvailable {
            get
            {
                #if UNITY_IOS || UNITY_ANDROID
                    #if UNITY_EDITOR
                        return false; // StepCounter not available in editor
                    #else
                        return StepCounter.current != null;
                    #endif
                #else
                    return false;
                #endif
            }
        }
        
        public bool IsAuthorized => _isAuthorized;

        public bool SupportsRealTime => true;

        public bool SupportsHistoricalData => true;

        public event Action<int> OnStepsUpdated;

        public void RequestAuthorization(Action<bool> callback) {
            Debug.Log("[InputSystemStepProvider] Requesting authorization...");
            
        #if UNITY_ANDROID && !UNITY_EDITOR
            RequestAndroidPermission(callback);
        #elif UNITY_IOS && !UNITY_EDITOR
            CheckStepCounterAvailability(callback);
        #else
            Debug.Log("[InputSystemStepProvider] Platform not supported for step counting");
            _isAuthorized = false;
            callback?.Invoke(false);
        #endif
        }
        
        
        public void GetStepsSince(DateTime since, Action<StepQueryData> callback) {
            callback?.Invoke(StepQueryData.Failed(
                "Historical step data not supported by Input System. Use real-time tracking instead.",
                Source
            ));
        }

        public void StartRealTimeTracking() {
            if (_isTracking)
                return;
            
        #if (UNITY_IOS || UNITY_ANDROID) && !UNITY_EDITOR
            _stepCounter = StepCounter.current;
            
            if (_stepCounter == null) {
                Debug.Log("[InputSystemStepProvider] StepCounter device not found. Is pedometer hardware available?");
                return;
            }

            if (!_stepCounter.enabled) {
                InputSystem.EnableDevice(_stepCounter);
                Debug.Log("[InputSystemStepProvider] Enabled StepCounter device");
            }

            _baselineSteps = _stepCounter.stepCounter.ReadValue();
            _lastReadSteps = _baselineSteps;
            _sessionSteps = 0;
            _isTracking = true;

            Debug.Log($"[InputSystemStepProvider] Started real-time tracking. Baseline steps: {_baselineSteps}");
        #else
            Debug.Log("[InputSystemStepProvider] Real-time step tracking not available on this platform");
        #endif
        }

        public void StopRealTimeTracking() {
            if (!_isTracking)
                return;

            Debug.Log($"[InputSystemStepProvider] Stopped tracking. Session steps: {_sessionSteps}");
            _isTracking = false;
        }

        #endregion

        #region Unity Lifecycle

        private void Update() {
            if (!_isTracking || _stepCounter == null)
                return;
            
            int currentSteps = _stepCounter.stepCounter.ReadValue();

            if (currentSteps == _lastReadSteps) 
                return;
            
            int delta = currentSteps - _lastReadSteps;

            // Steps should only increase unless the device rebooted, in which case delta could be negative
            if (delta > 0) {
                _sessionSteps += delta;
                _lastReadSteps = currentSteps;
                
                Debug.Log($"[InputSystemStepProvider] Steps detected: +{delta} (session total: {_sessionSteps})");
                
                OnStepsUpdated?.Invoke(delta);
            }
            else if (delta < 0) {
                Debug.Log($"[InputSystemStepProvider] Detected device reboot (delta={delta}). Resetting baseline.");
                
                _baselineSteps = currentSteps;
                _lastReadSteps = currentSteps;
            }
        }

        private void OnDestroy() {
            StopRealTimeTracking();
        }

        private void OnApplicationPause(bool pauseStatus) {
            if (pauseStatus) {
                Debug.Log("[InputSystemStepProvider] App paused - step tracking suspended.");
            }
            else {
                if (_isTracking && _stepCounter != null) {
                    int currentSteps = _stepCounter.stepCounter.ReadValue();
                    Debug.Log($"[InputSystemStepProvider] App resumed. Steps while paused: {currentSteps - _lastReadSteps} (not counted)");
                    _lastReadSteps = currentSteps;
                }
            }
        }

        #endregion
        
        #region Platform-Specific Authorization

#if UNITY_ANDROID && !UNITY_EDITOR
        private void RequestAndroidPermission(Action<bool> callback) {
            const string permission = "android.permission.ACTIVITY_RECOGNITION";
            
            if (Permission.HasUserAuthorizedPermission(permission)) {
                Debug.Log("[InputSystemStepProvider] ACTIVITY_RECOGNITION permission already granted");
                CheckStepCounterAvailability(callback);
            }
            else {
                Debug.Log("[InputSystemStepProvider] Requesting ACTIVITY_RECOGNITION permission...");
                
                PermissionCallbacks callbacks = new();
                callbacks.PermissionGranted += (perm) => {
                    Debug.Log($"[InputSystemStepProvider] Permission granted: {perm}");
                    CheckStepCounterAvailability(callback);
                };
                
                callbacks.PermissionDenied += (perm) => {
                    Debug.LogWarning($"[InputSystemStepProvider] Permission denied: {perm}");
                    _isAuthorized = false;
                    callback?.Invoke(false);
                };
                
                Permission.RequestUserPermission(permission, callbacks);
            }
        }
#endif

        private void CheckStepCounterAvailability(Action<bool> callback) {
        #if (UNITY_IOS || UNITY_ANDROID) && !UNITY_EDITOR
            var stepCounter = StepCounter.current;
            
            if (stepCounter != null) {
                Debug.Log("[InputSystemStepProvider] StepCounter device is available");
                _isAuthorized = true;
                callback?.Invoke(true);
            }
            else {
                Debug.LogWarning("[InputSystemStepProvider] StepCounter device not available on this device");
                _isAuthorized = false;
                callback?.Invoke(false);
            }
        #else
            _isAuthorized = false;
            callback?.Invoke(false);
        #endif
        }
        
        #endregion

        #region Helper Methods

        /// <summary>
        /// Reset the session step counter to zero
        /// </summary>
        public void ResetSessionSteps() {
            Debug.Log($"[InputSystemStepProvider] Resetting session steps (was {_sessionSteps})");
            _sessionSteps = 0;

            if (_isTracking && _stepCounter != null) {
                _baselineSteps = _stepCounter.stepCounter.ReadValue();
                _lastReadSteps = _baselineSteps;
            }
        }

        #endregion
    }
}