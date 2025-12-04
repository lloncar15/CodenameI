using System;
using System.Collections;
using UnityEngine;

namespace GimGim.StepTracking {
    public class StepDataManager : MonoBehaviour {
        #region Singleton
        
        public static StepDataManager Instance { get; private set; }
        
        #endregion

        #region Inspector Fields

        [Header("Settings")]
        [Tooltip("Automatically start tracking when initialized")]
        [SerializeField] private bool autoStartTracking = false;
        
        [Tooltip("Enable verbose debug logging")]
        [SerializeField] private bool enableDebugLogs = true;
        
        #endregion

        #region Private Fields
        
        private IStepProvider _activeProvider;
        private InputSystemStepProvider _inputSystemProvider;
        private StepDataPersistence _persistence;
        private bool _isInitialized = false;

        #endregion

        #region Constants

        private const float TIMEOUT_INTERVAL = 10f;

        #endregion

        #region Properties

        /// <summary>
        /// The currently active step source
        /// </summary>
        public StepSource ActiveSource => _activeProvider?.Source ?? StepSource.None;
        
        /// <summary>
        /// Whether the manager has been initialized
        /// </summary>
        public bool IsInitialized => _isInitialized;
        
        /// <summary>
        /// Whether step tracking is currently active
        /// </summary>
        public bool IsTracking => _inputSystemProvider?.IsTracking ?? false;
        
        /// <summary>
        /// Total points the player has accumulated
        /// </summary>
        public int TotalPoints => _persistence?.TotalPointsAwarded ?? 0;
        
        /// <summary>
        /// Steps taken today
        /// </summary>
        public int StepsToday => _persistence?.StepsToday ?? 0;
        
        /// <summary>
        /// Total steps ever recorded
        /// </summary>
        public long TotalStepsAllTime => _persistence?.TotalStepsAllTime ?? 0;

        #endregion

        #region Events

        /// <summary>
        /// Fired when points are awarded (passes the number of new points)
        /// </summary>
        public event Action<int> OnPointsAwarded;
        
        /// <summary>
        /// Fired when steps are detected (passes the number of new steps)
        /// </summary>
        public event Action<int> OnStepsDetected;
        
        /// <summary>
        /// Fired when the active step source changes
        /// </summary>
        public event Action<StepSource> OnSourceChanged;
        
        /// <summary>
        /// Fired when initialization completes (passes success status)
        /// </summary>
        public event Action<bool> OnInitialized;

        #endregion

        #region Unity Lifecycle

        private void Awake() {
            if (Instance != null && Instance != this) {
                Debug.Log("[StepDataManager] Duplicate StepDataManager detected, destroying...");
                Destroy(gameObject);
                return;
            }
            
            Instance = this;
            DontDestroyOnLoad(gameObject);
            
            _persistence = new StepDataPersistence();
            _persistence.Load();
            
            Debug.Log("[StepDataManager] StepDataManager Awake complete");
        }
        
        private void Start() {
            Initialize(success => {
                if (success && autoStartTracking) {
                    StartTracking();
                }
            });
        }
        
        private void OnDestroy() {
            if (Instance == this) {
                StopTracking();
                Instance = null;
            }
        }

        #endregion

        #region Initialization

        /// <summary>
        /// Initialize the step tracking system
        /// </summary>
        /// <param name="callback">Called when initialization completes</param>
        public void Initialize(Action<bool> callback = null) {
            if (_isInitialized) {
                Debug.Log("[StepDataManager] Already initialized");
                callback?.Invoke(true);
                return;
            }
            
            StartCoroutine(InitializeCoroutine(callback));
        }
        
        private IEnumerator InitializeCoroutine(Action<bool> callback) {
            Debug.Log("[StepDataManager] Starting initialization...");
            
            _inputSystemProvider = gameObject.AddComponent<InputSystemStepProvider>();
            
            // Wait a frame for the component to initialize
            yield return null;
            
            // Check availability
            if (_inputSystemProvider.IsAvailable) {
                Debug.Log("[StepDataManager] Input System StepCounter is available");
                _activeProvider = _inputSystemProvider;
            }
            else {
                Debug.Log("[StepDataManager] Input System StepCounter is NOT available on this device/platform");
            }
            
            if (_activeProvider != null) {
                bool authComplete = false;
                bool authResult = false;
                
                _activeProvider.RequestAuthorization(result => {
                    authResult = result;
                    authComplete = true;
                });
                
                float elapsed = 0f;
                while (!authComplete && elapsed < TIMEOUT_INTERVAL) {
                    elapsed += Time.deltaTime;
                    yield return null;
                }
                
                if (!authComplete) {
                    Debug.LogWarning("[StepDataManager] Authorization timed out");
                }
                else if (authResult) {
                    Debug.Log($"[StepDataManager] Authorization granted for {_activeProvider.Source}");
                    
                    _activeProvider.OnStepsUpdated += HandleStepsDetected;
                }
                else {
                    Debug.LogWarning("[StepDataManager] Authorization denied or unavailable");
                    _activeProvider = null;
                }
            }
            
            _isInitialized = true;
            bool success = _activeProvider != null && _activeProvider.IsAuthorized;
            
            Debug.Log($"[StepDataManager] Initialization complete. Success: {success}, Source: {ActiveSource}");
            
            OnSourceChanged?.Invoke(ActiveSource);
            OnInitialized?.Invoke(success);
            callback?.Invoke(success);
        }

        #endregion

        #region Tracking Control

        /// <summary>
        /// Start real-time step tracking
        /// </summary>
        public void StartTracking() {
            if (!_isInitialized) {
                Debug.LogWarning("[StepDataManager] Cannot start tracking - not initialized");
                return;
            }
            
            if (_activeProvider == null) {
                Debug.LogWarning("[StepDataManager] Cannot start tracking - no provider available");
                return;
            }
            
            if (!_activeProvider.SupportsRealTime) {
                Debug.LogWarning("[StepDataManager] Active provider does not support real-time tracking");
                return;
            }
            
            Debug.Log("[StepDataManager] Starting real-time tracking...");
            _activeProvider.StartRealTimeTracking();
        }
        
        /// <summary>
        /// Stop real-time step tracking
        /// </summary>
        public void StopTracking() {
            if (_activeProvider == null) 
                return;
            
            Debug.Log("[StepDataManager] Stopping tracking...");
            _activeProvider.StopRealTimeTracking();
        }
        
        /// <summary>
        /// Toggle tracking on/off
        /// </summary>
        public void ToggleTracking() {
            if (IsTracking) {
                StopTracking();
            }
            else {
                StartTracking();
            }
        }

        #endregion

        #region Steo Handling

        private void HandleStepsDetected(int steps) {
            if (steps <= 0) 
                return;
            
            Debug.Log($"[StepDataManager] Steps detected: {steps}");
            
            int points = _persistence.RecordSteps(steps, ActiveSource);
            OnStepsDetected?.Invoke(steps);
            
            if (points > 0) {
                OnPointsAwarded?.Invoke(points);
            }
        }

        #endregion

        #region Public API

         /// <summary>
        /// Manually add steps (for testing or manual entry)
        /// </summary>
        /// <param name="steps">Number of steps to add</param>
        /// <returns>Points awarded</returns>
        public int AddStepsManually(int steps) {
            if (steps <= 0) 
                return 0;
            
            Debug.Log($"[StepDataManager] Manually adding {steps} steps");
            
            int points = _persistence.RecordSteps(steps, StepSource.None);
            
            OnStepsDetected?.Invoke(steps);
            if (points > 0) {
                OnPointsAwarded?.Invoke(points);
            }
            
            return points;
        }
        
        /// <summary>
        /// Spend points (for purchasing items, opening boxes, etc.)
        /// </summary>
        /// <param name="amount">Points to spend</param>
        /// <returns>True if successful, false if insufficient points</returns>
        public bool SpendPoints(int amount) {
            if (amount <= 0) 
                return true;
            
            if (_persistence.TotalPointsAwarded >= amount) {
                _persistence.TotalPointsAwarded -= amount;
                _persistence.Save();
                Debug.Log($"[StepDataManager] Spent {amount} points. Remaining: {_persistence.TotalPointsAwarded}");
                return true;
            }
            
            Debug.Log($"[StepDataManager] Cannot spend {amount} points - only have {_persistence.TotalPointsAwarded}");
            return false;
        }
        
        /// <summary>
        /// Add points directly (for bonuses, passive income, etc.)
        /// </summary>
        /// <param name="amount">Points to add</param>
        public void AddPoints(int amount) {
            if (amount <= 0) 
                return;
            
            _persistence.TotalPointsAwarded += amount;
            _persistence.Save();
            
            Debug.Log($"[StepDataManager] Added {amount} bonus points. Total: {_persistence.TotalPointsAwarded}");
            OnPointsAwarded?.Invoke(amount);
        }
        
        /// <summary>
        /// Get current session steps (since tracking started)
        /// </summary>
        public int GetSessionSteps() {
            return _inputSystemProvider?.GetSessionSteps() ?? 0;
        }
        
        /// <summary>
        /// Force save current state
        /// </summary>
        public void ForceSave() {
            _persistence?.Save();
        }
        
        /// <summary>
        /// Clear all saved data (for testing/reset)
        /// </summary>
        public void ClearAllData() {
            _persistence?.ClearAll();
            Debug.Log("[StepDataManager] All data cleared");
        }

        #endregion
        
        #region Debug / Testing
        
        /// <summary>
        /// Get a debug status string
        /// </summary>
        public string GetDebugStatus() {
            return $"Initialized: {_isInitialized}\n" +
                   $"Source: {ActiveSource}\n" +
                   $"Tracking: {IsTracking}\n" +
                   $"Authorized: {_activeProvider?.IsAuthorized ?? false}\n" +
                   $"Session Steps: {GetSessionSteps()}\n" +
                   $"Today's Steps: {StepsToday}\n" +
                   $"Total Steps: {TotalStepsAllTime}\n" +
                   $"Total Points: {TotalPoints}";
        }
        
        #endregion
    }
}