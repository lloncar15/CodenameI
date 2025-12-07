using System;
using UnityEngine;

namespace GimGim.StepTracking.StepTracking {
    /// <summary>
    /// Unity wrapper for iOS HealthKit API
    /// This class handles communication with the native iOS plugin
    /// </summary>
    public class HealthKitProvider : MonoBehaviour, IStepProvider {
        #region Native Plugin Imports
        
#if UNITY_IOS && !UNITY_EDITOR
        [DllImport("__Internal")]
        private static extern int HealthKit_IsAvailable();
        
        [DllImport("__Internal")]
        private static extern int HealthKit_IsAuthorized();
        
        [DllImport("__Internal")]
        private static extern void HealthKit_RequestAuthorization();
        
        [DllImport("__Internal")]
        private static extern void HealthKit_GetStepsSince(long timestampMillis);
        
        [DllImport("__Internal")]
        private static extern void HealthKit_GetStepsForRange(long startMillis, long endMillis);
        
        [DllImport("__Internal")]
        private static extern void HealthKit_GetStepsToday();
        
        [DllImport("__Internal")]
        private static extern void HealthKit_OpenHealthApp();
#endif
        
        #endregion
        
        #region Singleton
        
        private static HealthKitProvider _instance;
        
        public static HealthKitProvider Instance {
            get {
                if (_instance == null) {
                    _instance = FindFirstObjectByType<HealthKitProvider>();
                    
                    if (_instance == null) {
                        GameObject go = new("HealthKitProvider");
                        _instance = go.AddComponent<HealthKitProvider>();
                        DontDestroyOnLoad(go);
                    }
                }
                return _instance;
            }
        }
        
        #endregion
        
        #region Private Fields
        
        private Action<bool> _authorizationCallback;
        private Action<StepData> _stepDataCallback;
        
        #endregion
        
        #region Events
        
        /// <summary>
        /// Fired when authorization request completes
        /// </summary>
        public event Action<bool> OnAuthorizationComplete;
        
        /// <summary>
        /// Fired when steps are received from a query
        /// </summary>
        public event Action<HealthKitStepResult> OnStepsQueried;
        
        /// <summary>
        /// Fired when an error occurs
        /// </summary>
        public event Action<string, string> OnError;
        
        // IStepProvider event
        public event Action<int> OnStepsUpdated;
        
        #endregion
        
        #region IStepProvider Implementation
        
        public StepSource Source => StepSource.HealthKit;
        
        public bool IsAvailable {
            get {
            #if UNITY_IOS && !UNITY_EDITOR
                return HealthKit_IsAvailable() == 1;
            #else
                return false;
            #endif
            }
        }
        
        public bool IsAuthorized {
            get {
            #if UNITY_IOS && !UNITY_EDITOR
                return HealthKit_IsAuthorized() == 1;
            #else
                return false;
            #endif
            }
        }
        
        public bool SupportsRealTime => false;
        
        public bool SupportsHistoricalData => true;
        
        public void RequestAuthorization(Action<bool> callback) {
            _authorizationCallback = callback;
            RequestPermissions();
        }
        
        public void GetStepsSince(DateTime since, Action<StepData> callback) {
            _stepDataCallback = callback;
            long sinceMillis = new DateTimeOffset(since.ToUniversalTime()).ToUnixTimeMilliseconds();
            QueryStepsSince(sinceMillis);
        }
        
        public void StartRealTimeTracking() {
            // No-op
        }
        
        public void StopRealTimeTracking() {
            // No-op
        }
        
        #endregion
        
        #region Unity Lifecycle
        
        /// <summary>
        /// The GameObject must be named HealthKitReceiver for Unity to receive messages from native iOS.
        /// </summary>
        private void Awake() {
            if (_instance != null && _instance != this) {
                Destroy(gameObject);
                return;
            }
            
            _instance = this;
            DontDestroyOnLoad(gameObject);
            
            gameObject.name = "HealthKitReceiver";
        }
        
        private void OnDestroy() {
            if (_instance == this) {
                _instance = null;
            }
        }
        
        #endregion
        
         #region Public Methods
        
        /// <summary>
        /// Check if HealthKit is available on this device
        /// </summary>
        public bool CheckAvailability() {
        #if UNITY_IOS && !UNITY_EDITOR
            bool available = HealthKit_IsAvailable() == 1;
            Debug.Log($"[HealthKitProvider] Availability: {available}");
            return available;
        #else
            Debug.Log("[HealthKitProvider] HealthKit only available on iOS device");
            return false;
        #endif
        }
        
        /// <summary>
        /// Check authorization status
        /// Returns: 1 = authorized, 0 = denied, -1 = not determined
        /// </summary>
        public int CheckAuthorizationStatus() {
        #if UNITY_IOS && !UNITY_EDITOR
            return HealthKit_IsAuthorized();
        #else
            return -1;
        #endif
        }
        
        /// <summary>
        /// Request HealthKit permissions
        /// </summary>
        public void RequestPermissions() {
        #if UNITY_IOS && !UNITY_EDITOR
            Debug.Log("[HealthKitProvider] Requesting permissions...");
            HealthKit_RequestAuthorization();
        #else
            Debug.Log("[HealthKitProvider] Permissions not available in editor");
            _authorizationCallback?.Invoke(false);
        #endif
        }
        
        /// <summary>
        /// Query steps since a given timestamp (milliseconds since epoch)
        /// </summary>
        public void QueryStepsSince(long timestampMillis) {
        #if UNITY_IOS && !UNITY_EDITOR
            Debug.Log($"[HealthKitProvider] Querying steps since {timestampMillis}");
            HealthKit_GetStepsSince(timestampMillis);
        #else
            Debug.Log("[HealthKitProvider] Step query not available in editor");
        #endif
        }
        
        /// <summary>
        /// Query steps for a date range
        /// </summary>
        public void QueryStepsForRange(DateTime start, DateTime end) {
        #if UNITY_IOS && !UNITY_EDITOR
            long startMillis = new DateTimeOffset(start.ToUniversalTime()).ToUnixTimeMilliseconds();
            long endMillis = new DateTimeOffset(end.ToUniversalTime()).ToUnixTimeMilliseconds();
            
            Debug.Log($"[HealthKitProvider] Querying steps for range {start} to {end}");
            HealthKit_GetStepsForRange(startMillis, endMillis);
        #else
            Debug.Log("[HealthKitProvider] Step query not available in editor");
        #endif
        }
        
        /// <summary>
        /// Query steps for today
        /// </summary>
        public void QueryStepsToday() {
        #if UNITY_IOS && !UNITY_EDITOR
            Debug.Log("[HealthKitProvider] Querying today's steps");
            HealthKit_GetStepsToday();
        #else
            Debug.Log("[HealthKitProvider] Step query not available in editor");
        #endif
        }
        
        /// <summary>
        /// Open the iOS Health app
        /// </summary>
        public void OpenHealthApp() {
        #if UNITY_IOS && !UNITY_EDITOR
            HealthKit_OpenHealthApp();
        #else
            Debug.Log("[HealthKitProvider] Cannot open Health app in editor");
        #endif
        }
        
        #endregion
        
        #region Native Callbacks
        
        // These methods are called by the native iOS plugin via UnitySendMessage
        // The GameObject must be named "HealthKitReceiver" for this to work
        
        /// <summary>
        /// Called when authorization request completes
        /// </summary>
        public void OnAuthorizationResult(string result) {
            Debug.Log($"[HealthKitProvider] OnAuthorizationResult: {result}");
            
            bool authorized = result.ToLower() == "true";
            
            OnAuthorizationComplete?.Invoke(authorized);
            _authorizationCallback?.Invoke(authorized);
            _authorizationCallback = null;
        }
        
        /// <summary>
        /// Called when steps are received. Converts the data to StepData for IStepProvider callback
        /// and fires the real-time event for compatibility
        /// </summary>
        public void OnStepsReceived(string jsonResult) {
            Debug.Log($"[HealthKitProvider] OnStepsReceived: {jsonResult}");
            
            try {
                HealthKitStepResult result = JsonUtility.FromJson<HealthKitStepResult>(jsonResult);
                OnStepsQueried?.Invoke(result);

                if (_stepDataCallback == null || !result.success) 
                    return;

                StepData stepData = StepData.Succeeded(
                    (int)result.steps,
                    DateTimeOffset.FromUnixTimeMilliseconds(result.startTime).DateTime,
                    DateTimeOffset.FromUnixTimeMilliseconds(result.endTime).DateTime,
                    StepSource.HealthKit);
                    
                _stepDataCallback.Invoke(stepData);
                _stepDataCallback = null;
                
                OnStepsUpdated?.Invoke((int)result.steps);
            }
            catch (Exception e) {
                Debug.LogError($"[HealthKitProvider] Failed to parse step result: {e.Message}");
            }
        }
        
        /// <summary>
        /// Called when an error occurs. If we expected callback for step data, send it with failed data.
        /// </summary>
        public void OnHealthKitError(string jsonError) {
            Debug.LogError($"[HealthKitProvider] OnHealthKitError: {jsonError}");
            
            try {
                HealthKitStepResult error = JsonUtility.FromJson<HealthKitStepResult>(jsonError);
                OnError?.Invoke(error.errorCode, error.errorMessage);
                
                if (_stepDataCallback == null) 
                    return;
                
                StepData stepData = StepData.Failed(
                    $"{error.errorCode}: {error.errorMessage}",
                    StepSource.HealthKit);
                
                _stepDataCallback.Invoke(stepData);
                _stepDataCallback = null;
            }
            catch (Exception e) {
                Debug.LogError($"[HealthKitProvider] Failed to parse error: {e.Message}");
            }
        }
        
        #endregion
    }
}