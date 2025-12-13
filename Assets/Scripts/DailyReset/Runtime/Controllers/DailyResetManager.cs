using System;
using System.Collections.Generic;
using UnityEngine;

namespace GimGim.DailyReset {
    /// <summary>
    /// Manages daily reset logic and notifies subscribers when a reset is due.
    /// Checks are performed on app launch (and can be expanded to periodic checks).
    /// </summary>
    public class DailyResetManager : MonoBehaviour {
        #region Singleton

        public static DailyResetManager Instance { get; private set; }

        #endregion

        #region Inspector Fields

        [Header("Reset Settings")]
        [Tooltip("Hour of the day (0-23) when daily reset occurs in local time")]
        [SerializeField] private int resetHour = 4;

        [Tooltip("Minute of the hour when daily reset occurs")]
        [SerializeField] private int resetMinute = 0;

        #endregion
        
        #region Private Fields

        private DailyResetPersistentData _persistentData;
        private readonly SortedList<int, Action> _resetHandlers = new SortedList<int, Action>();

        #endregion

        #region Properties

        /// <summary>
        /// The hour (0-23) at which daily reset occurs in local time
        /// </summary>
        public int ResetHour => resetHour;

        /// <summary>
        /// The minute at which daily reset occurs
        /// </summary>
        public int ResetMinute => resetMinute;

        /// <summary>
        /// The last time a daily reset was performed
        /// </summary>
        public DateTime LastResetTime => _persistentData?.LastResetTime ?? DateTime.MinValue;

        #endregion
        
        #region Events

        /// <summary>
        /// Fired when a daily reset occurs. Use RegisterResetHandler for ordered execution.
        /// </summary>
        public event Action OnDailyReset;

        #endregion
        
        #region Unity Lifecycle

        private void Awake() {
            if (Instance != null && Instance != this) {
                Debug.LogWarning("[DailyResetManager] Duplicate instance detected, destroying...");
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            _persistentData = new DailyResetPersistentData();
            _persistentData.Load();

            Debug.Log($"[DailyResetManager] Initialized. Last reset: {_persistentData.LastResetTime}");
        }

        private void Start() {
            CheckAndPerformReset();
        }

        private void OnDestroy() {
            if (Instance == this) {
                Instance = null;
            }
        }

        #endregion
        
        #region Public API

        /// <summary>
        /// Register a handler to be called during daily reset with a specific priority.
        /// Lower priority values execute first.
        /// </summary>
        /// <param name="priority">Execution order (lower = earlier). Use unique values.</param>
        /// <param name="handler">The action to invoke on reset</param>
        public void RegisterResetHandler(int priority, Action handler) {
            if (handler == null) {
                Debug.LogWarning("[DailyResetManager] Cannot register null handler");
                return;
            }

            if (_resetHandlers.TryAdd(priority, handler))
                Debug.Log($"[DailyResetManager] Registered reset handler with priority {priority}");
            else
                Debug.LogWarning($"[DailyResetManager] Priority {priority} already registered. Use a unique priority.");
        }

        /// <summary>
        /// Unregister a previously registered reset handler by priority
        /// </summary>
        /// <param name="priority">The priority of the handler to remove</param>
        public void UnregisterResetHandler(int priority) {
            if (_resetHandlers.Remove(priority)) {
                Debug.Log($"[DailyResetManager] Unregistered reset handler with priority {priority}");
            }
        }

        /// <summary>
        /// Manually trigger a reset check. Useful for testing or after app resume.
        /// </summary>
        public void CheckAndPerformReset() {
            if (!IsResetDue()) {
                Debug.Log("[DailyResetManager] No reset due");
                return;
            }

            PerformReset();
        }

        /// <summary>
        /// Configure the reset time. Changes take effect on next reset check.
        /// </summary>
        /// <param name="hour">Hour (0-23) in local time</param>
        /// <param name="minute">Minute (0-59)</param>
        public void SetResetTime(int hour, int minute) {
            resetHour = Mathf.Clamp(hour, 0, 23);
            resetMinute = Mathf.Clamp(minute, 0, 59);
            Debug.Log($"[DailyResetManager] Reset time changed to {resetHour:D2}:{resetMinute:D2}");
        }

        /// <summary>
        /// Force a reset regardless of timing. Use for testing only.
        /// </summary>
        public void ForceReset() {
            Debug.Log("[DailyResetManager] Forcing reset...");
            PerformReset();
        }

        /// <summary>
        /// Get the next scheduled reset time
        /// </summary>
        /// <returns>DateTime of the next reset in local time</returns>
        public DateTime GetNextResetTime() {
            DateTime now = DateTime.Now;
            DateTime todayReset = new DateTime(now.Year, now.Month, now.Day, resetHour, resetMinute, 0);

            return now < todayReset ? todayReset : todayReset.AddDays(1);
        }

        #endregion
        
        #region Private Methods

        /// <summary>
        /// Determines if a daily reset is due based on the configured reset time
        /// </summary>
        /// <returns>True if reset should be performed</returns>
        private bool IsResetDue() {
            DateTime now = DateTime.Now;
            DateTime lastReset = _persistentData.LastResetTime;
            
            if (lastReset == DateTime.MinValue) {
                Debug.Log("[DailyResetManager] First launch detected, reset is due");
                return true;
            }

            DateTime lastResetBoundary = GetResetBoundaryFor(lastReset);
            DateTime currentResetBoundary = GetResetBoundaryFor(now);
            
            bool isDue = currentResetBoundary > lastResetBoundary;
            
            Debug.Log($"[DailyResetManager] Reset check - Last boundary: {lastResetBoundary}, Current boundary: {currentResetBoundary}, Due: {isDue}");
            
            return isDue;
        }

        /// <summary>
        /// Gets the reset boundary datetime for a given datetime.
        /// The boundary is the most recent reset time at or before the given datetime.
        /// </summary>
        /// <param name="dateTime">The datetime to calculate boundary for</param>
        /// <returns>The reset boundary datetime</returns>
        private DateTime GetResetBoundaryFor(DateTime dateTime) {
            DateTime sameDayReset = new DateTime(dateTime.Year, dateTime.Month, dateTime.Day, resetHour, resetMinute, 0);
            
            return dateTime < sameDayReset ? sameDayReset.AddDays(-1) : sameDayReset;
        }

        /// <summary>
        /// Executes all registered reset handlers in priority order and fires the reset event
        /// </summary>
        private void PerformReset() {
            Debug.Log($"[DailyResetManager] Performing daily reset. {_resetHandlers.Count} handlers registered.");
            
            foreach (KeyValuePair<int, Action> kvp in _resetHandlers) {
                try {
                    Debug.Log($"[DailyResetManager] Executing handler with priority {kvp.Key}");
                    kvp.Value.Invoke();
                }
                catch (Exception e) {
                    Debug.LogError($"[DailyResetManager] Handler with priority {kvp.Key} threw exception: {e.Message}");
                }
            }
            
            try {
                OnDailyReset?.Invoke();
            }
            catch (Exception e) {
                Debug.LogError($"[DailyResetManager] OnDailyReset event handler threw exception: {e.Message}");
            }
            
            _persistentData.LastResetTime = DateTime.Now;
            _persistentData.Save();

            Debug.Log($"[DailyResetManager] Daily reset complete. Next reset: {GetNextResetTime()}");
        }

        #endregion

    }
}
