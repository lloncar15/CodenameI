using System;
using UnityEngine;

namespace GimGim.StepTracking.StepTracking {
    /// <summary>
    /// Unity wrapper for Android Health Connect API
    /// This class handles communication with the native Android plugin
    /// </summary>
    public class HealthConnectProvider : MonoBehaviour, IStepProvider {
        #region Singleton

        private static HealthConnectProvider _instance;

        public static HealthConnectProvider Instance {
            get {
                if (_instance == null) {
                    _instance = FindFirstObjectByType<HealthConnectProvider>();

                    if (_instance == null) {
                        GameObject go = new GameObject("HealthConnectProvider");
                        _instance = go.AddComponent<HealthConnectProvider>();
                        DontDestroyOnLoad(go);
                    }
                }
                return _instance;
            }
        }

        #endregion

        #region Events

        /// <summary>
        /// Fired when Health Connect is initialized
        /// </summary>
        public event Action<bool> OnInitialized;
        
        /// <summary>
        /// Fired when permission request completes
        /// </summary>
        public event Action<bool> OnPermissionsGranted;
        
        /// <summary>
        /// Fired when steps are received from a query
        /// </summary>
        public event Action<HealthConnectStepResult> OnStepsQueried;
        
        /// <summary>
        /// Fired when an error occurs
        /// </summary>
        public event Action<string, string> OnError;
        
        // IStepProvider event
        public event Action<int> OnStepsUpdated;

        #endregion

        #region IStepProvider Implementation

        public StepSource Source => StepSource.HealthConnect;

        public bool IsAvailable {
            get {
            #if UNITY_ANDROID && !UNITY_EDITOR
                return CheckAvailability() == HealthConnectAvailability.Available;
            #else
                return false;
            #endif
            }
        }

        public bool IsAuthorized => _hasPermissions;
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

        #region Private Fields

        private bool _isInitialized = false;
        private bool _hasPermissions = false;
        private Action<bool> _authorizationCallback;
        private Action<StepData> _stepDataCallback;
        
// #if UNITY_ANDROID && !UNITY_EDITOR
        private AndroidJavaClass _bridgeClass;
// #endif

        #endregion

        #region Unity Lifecycle

        /// <summary>
        /// The GameObject must be named HealthConnectReceiver for Unity to receive messages from native Android.
        /// </summary>
        private void Awake() {
            if (_instance != null && _instance != this) {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            DontDestroyOnLoad(gameObject);
            
            gameObject.name = "HealthConnectReceiver";
        }

        private void OnDestroy() {
            if (_instance == this) {
                _instance = null;
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Initialize Health Connect
        /// </summary>
        public void Initialize() {
        #if UNITY_ANDROID && !UNITY_EDITOR
            try {
                _bridgeClass = new AndroidJavaClass("com.gimgim.codenamei.healthconnect.HealthConnectBridge");
                _bridgeClass.CallStatic("initialize");
            }
            catch (Exception e) {
                Debug.LogError($"[HealthConnectProvider] Failed to initialize: {e.Message}");
                OnInitialized?.Invoke(false);
            }
        #else
            Debug.Log("[HealthConnectProvider] Health Connect only available on Android device");
            OnInitialized?.Invoke(false);
        #endif
        }

        /// <summary>
        /// Check if Health Connect is available on this device
        /// </summary>
        public HealthConnectAvailability CheckAvailability() {
        #if UNITY_ANDROID && !UNITY_EDITOR
            try {
                if (_bridgeClass == null) {
                    _bridgeClass = new AndroidJavaClass("com.gimgim.codenamei.healthconnect.HealthConnectBridge");
                }
                
                int result = _bridgeClass.CallStatic<int>("checkAvailability");
                return (HealthConnectAvailability)result;
            }
            catch (Exception e) {
                Debug.LogError($"[HealthConnectProvider] Failed to check availability: {e.Message}");
                return HealthConnectAvailability.Unavailable;
            }
        #else
            return HealthConnectAvailability.Unavailable;
        #endif
        }

        /// <summary>
        /// Request Health Connect permissions
        /// </summary>
        public void RequestPermissions() {
        #if UNITY_ANDROID && !UNITY_EDITOR
            try {
                _bridgeClass?.CallStatic("requestPermissions");
                Debug.Log("[HealthConnectProvider] Permission request sent");
            }
            catch (Exception e) {
                Debug.LogError($"[HealthConnectProvider] Failed to request permissions: {e.Message}");
                _authorizationCallback?.Invoke(false);
            }
        #else
            Debug.Log("[HealthConnectProvider] Permissions not available in editor");
            _authorizationCallback?.Invoke(false);
        #endif
        }

        /// <summary>
        /// Check if permissions are granted
        /// </summary>
        public bool CheckPermissions() {
        #if UNITY_ANDROID && !UNITY_EDITOR
            try {
                _hasPermissions = _bridgeClass?.CallStatic<bool>("checkPermissions") ?? false;
                return _hasPermissions;
            }
            catch (Exception e) {
                Debug.LogError($"[HealthConnectProvider] Failed to check permissions: {e.Message}");
                return false;
            }
        #else
            return false;
        #endif
        }

        /// <summary>
        /// Query steps since a given timestamp (milliseconds since epoch)
        /// </summary>
        public void QueryStepsSince(long timestampMillis) {
        #if UNITY_ANDROID && !UNITY_EDITOR
            try {
                _bridgeClass?.CallStatic("getStepsSince", timestampMillis);
                Debug.Log($"[HealthConnectProvider] Querying steps since {timestampMillis}");
            }
            catch (Exception e) {
                Debug.LogError($"[HealthConnectProvider] Failed to query steps: {e.Message}");
            }
        #else
            Debug.Log("[HealthConnectProvider] Step query not available in editor");
        #endif
        }

        /// <summary>
        /// Query steps for a date range
        /// </summary>
        public void QueryStepsForRange(DateTime start, DateTime end) {
        #if UNITY_ANDROID && !UNITY_EDITOR
            try {
                long startMillis = new DateTimeOffset(start.ToUniversalTime()).ToUnixTimeMilliseconds();
                long endMillis = new DateTimeOffset(end.ToUniversalTime()).ToUnixTimeMilliseconds();
                
                _bridgeClass?.CallStatic("getStepsForDateRange", startMillis, endMillis);
                Debug.Log($"[HealthConnectProvider] Querying steps for range {start} to {end}");
            }
            catch (Exception e) {
                Debug.LogError($"[HealthConnectProvider] Failed to query steps for range: {e.Message}");
            }
        #else
            Debug.Log("[HealthConnectProvider] Step query not available in editor");
        #endif
        }

        /// <summary>
        /// Query steps for today
        /// </summary>
        public void QueryStepsToday() {
        #if UNITY_ANDROID && !UNITY_EDITOR
            try {
                _bridgeClass?.CallStatic("getStepsToday");
                Debug.Log("[HealthConnectProvider] Querying today's steps");
            }
            catch (Exception e) {
                Debug.LogError($"[HealthConnectProvider] Failed to query today's steps: {e.Message}");
            }
        #else
            Debug.Log("[HealthConnectProvider] Step query not available in editor");
        #endif
        }

        /// <summary>
        /// Open Health Connect settings
        /// </summary>
        public void OpenSettings() {
        #if UNITY_ANDROID && !UNITY_EDITOR
            try {
                _bridgeClass?.CallStatic("openHealthConnectSettings");
            }
            catch (Exception e) {
                Debug.LogError($"[HealthConnectProvider] Failed to open settings: {e.Message}");
            }
        #endif
        }

        /// <summary>
        /// Open Play Store to install Health Connect
        /// </summary>
        public void OpenPlayStore() {
        #if UNITY_ANDROID && !UNITY_EDITOR
            try {
                _bridgeClass?.CallStatic("openPlayStoreForHealthConnect");
            }
            catch (Exception e) {
                Debug.LogError($"[HealthConnectProvider] Failed to open Play Store: {e.Message}");
            }
        #else
            Application.OpenURL("https://play.google.com/store/apps/details?id=com.google.android.apps.healthdata");
        #endif
        }

        #endregion

        #region Native Callbacks

        // These methods are called by the native Android plugin via UnitySendMessage
        // The GameObject must be named "HealthConnectReceiver" for this to work

        /// <summary>
        /// Called when Health Connect is initialized
        /// </summary>
        public void OnHealthConnectInitialized(string result) {
            Debug.Log($"[HealthConnectProvider] OnHealthConnectInitialized: {result}");

            switch (result) {
                case "true":
                    _isInitialized = true;
                    OnInitialized?.Invoke(true);
                    break;
                case "needsUpdate":
                    _isInitialized = false;
                    Debug.LogWarning("[HealthConnectProvider] Health Connect needs to be installed or updated");
                    OnInitialized?.Invoke(false);
                    break;
                default:
                    _isInitialized = false;
                    OnInitialized?.Invoke(false);
                    break;
            }
        }

        /// <summary>
        /// Called when permissions result is received
        /// </summary>
        public void OnPermissionResult(string result) {
            Debug.Log($"[HealthConnectProvider] OnPermissionsResult: {result}");
            
            _hasPermissions = result.ToLower() == "true";
            OnPermissionsGranted?.Invoke(_hasPermissions);
            _authorizationCallback?.Invoke(_hasPermissions);
            _authorizationCallback = null;
        }

        /// <summary>
        /// Called when steps are received. Converts the data to StepData for IStepProvider callback
        /// and fires the real-time event for compatibility
        /// </summary>
        public void OnStepsReceived(string jsonResult) {
            Debug.Log($"[HealthConnectProvider] OnStepsReceived: {jsonResult}");
            
            try {
                HealthConnectStepResult result = JsonUtility.FromJson<HealthConnectStepResult>(jsonResult);
                OnStepsQueried?.Invoke(result);

                if (_stepDataCallback == null || !result.success) 
                    return;
                
                StepData stepData = StepData.Succeeded(
                    (int)result.steps,
                    DateTimeOffset.FromUnixTimeMilliseconds(result.startTime).DateTime,
                    DateTimeOffset.FromUnixTimeMilliseconds(result.endTime).DateTime,
                    StepSource.HealthConnect);
                    
                _stepDataCallback.Invoke(stepData);
                _stepDataCallback = null;
                    
                OnStepsUpdated?.Invoke((int)result.steps);
            }
            catch (Exception e) {
                Debug.LogError($"[HealthConnectProvider] Failed to parse step result: {e.Message}");
            }
        }

        /// <summary>
        /// Called when step records are received
        /// //TODO: check if this result should be parsed and its data used.
        /// </summary>
        public void OnStepRecordsReceived(string jsonResult) {
            Debug.Log($"[HealthConnectProvider] OnStepRecordsReceived: {jsonResult}");
        }

        /// <summary>
        /// Called when an error occurs
        /// </summary>
        public void OnHealthConnectError(string jsonError) {
            Debug.LogError($"[HealthConnectProvider] OnHealthConnectError: {jsonError}");
            
            try {
                HealthConnectStepResult error = JsonUtility.FromJson<HealthConnectStepResult>(jsonError);
                OnError?.Invoke(error.errorCode, error.errorMessage);

                if (_stepDataCallback == null) 
                    return;
                
                StepData stepData = StepData.Failed(
                    $"{error.errorCode}: {error.errorMessage}",
                    StepSource.HealthConnect);
                
                _stepDataCallback.Invoke(stepData);
                _stepDataCallback = null;
            }
            catch (Exception e) {
                Debug.LogError($"[HealthConnectProvider] Failed to parse error: {e.Message}");
            }
        }

        #endregion
    }
}