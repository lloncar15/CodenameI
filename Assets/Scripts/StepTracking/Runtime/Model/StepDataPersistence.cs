using System;
using UnityEngine;

namespace GimGim.StepTracking {
    /// <summary>
    /// Handles persistence of step data and game state to PlayerPrefs
    /// </summary>
    [Serializable]
    public class StepDataPersistence {
        #region Constants

        private const string PREFS_KEY = "WalkGame_StepData";

        #endregion

        #region Persisted Data

        /// <summary>
        /// Last time we synced steps from a health API
        /// </summary>
        public DateTime LastHealthSyncTimestamp { get; set; } = DateTime.UtcNow.AddDays(-1);
        
        /// <summary>
        /// Total points ever awarded to the player
        /// </summary>
        public int TotalPointsAwarded { get; set; } = 0;
        
        /// <summary>
        /// Steps accumulated from pedometer that haven't been "claimed" yet
        /// </summary>
        public int PendingPedometerSteps { get; set; } = 0;
        
        /// <summary>
        /// The step source that was last used
        /// </summary>
        public StepSource LastActiveSource { get; set; } = StepSource.None;
        
        /// <summary>
        /// Total steps ever counted
        /// </summary>
        public long TotalStepsAllTime { get; set; } = 0;
        
        /// <summary>
        /// Steps counted today
        /// </summary>
        public int StepsToday { get; set; } = 0;
        
        /// <summary>
        /// The date of the last step count (for daily reset)
        /// </summary>
        public DateTime LastStepDate { get; set; } = DateTime.MinValue;

        #endregion

        #region Save/Load

        /// <summary>
        /// Save current state to persistent storage
        /// </summary>
        public void Save() {
            try {
                SaveDataContainer saveData = new() {
                    lastHealthSyncTicks = LastHealthSyncTimestamp.Ticks,
                    totalPointsAwarded = TotalPointsAwarded,
                    pendingPedometerSteps = PendingPedometerSteps,
                    lastActiveSource = (int)LastActiveSource,
                    totalStepsAllTime = TotalStepsAllTime,
                    stepsToday = StepsToday,
                    lastStepDateTicks = LastStepDate.Ticks,
                    version = 1
                };
                
                string json = JsonUtility.ToJson(saveData);
                PlayerPrefs.SetString(PREFS_KEY, json);
                PlayerPrefs.Save();
                
                Debug.Log($"[StepDataPersistence] Saved: {TotalPointsAwarded} points, {TotalStepsAllTime} total steps");
            }
            catch (Exception e) {
                Debug.LogError($"[StepDataPersistence] Failed to save: {e.Message}");
            }
        }

        /// <summary>
        /// Load state from persistent storage
        /// </summary>
        public void Load() {
            try {
                if (!PlayerPrefs.HasKey(PREFS_KEY)) {
                    Debug.Log("[StepDataPersistence] No saved data found, using defaults");
                    return;
                }
                
                string json = PlayerPrefs.GetString(PREFS_KEY);
                SaveDataContainer saveData = JsonUtility.FromJson<SaveDataContainer>(json);
                
                LastHealthSyncTimestamp = new DateTime(saveData.lastHealthSyncTicks);
                TotalPointsAwarded = saveData.totalPointsAwarded;
                PendingPedometerSteps = saveData.pendingPedometerSteps;
                LastActiveSource = (StepSource)saveData.lastActiveSource;
                TotalStepsAllTime = saveData.totalStepsAllTime;
                StepsToday = saveData.stepsToday;
                LastStepDate = new DateTime(saveData.lastStepDateTicks);

                CheckDailyReset();
                
                Debug.Log($"[StepDataPersistence] Loaded: {TotalPointsAwarded} points, {TotalStepsAllTime} total steps");
            }
            catch (Exception e) {
                Debug.LogError($"[StepDataPersistence] Failed to load: {e.Message}");
                ResetToDefaults();
            }
        }

        /// <summary>
        /// Clear all saved data
        /// </summary>
        public void ClearAll() {
            PlayerPrefs.DeleteKey(PREFS_KEY);
            PlayerPrefs.Save();
            ResetToDefaults();
            Debug.Log("[StepDataPersistence] All data cleared");
        }

        #endregion

        #region Step Recording

        /// <summary>
        /// Record new steps and award points
        /// </summary>
        /// <param name="steps">Number of new steps to add</param>
        /// <param name="source">Where the steps came from</param>
        /// <returns>Points awarded for these steps</returns>
        public int RecordSteps(int steps, StepSource source) {
            if (steps <= 0)
                return 0;
            
            CheckDailyReset();
            
            TotalStepsAllTime += steps;
            StepsToday += steps;
            LastStepDate = DateTime.UtcNow.Date;
            LastActiveSource = source;

            int points = CalculatePoints(steps);
            TotalPointsAwarded += points;

            Save();
            
            return points;
        }

        /// <summary>
        /// Calculate points for a given number of steps
        /// TODO: move this to a step points calculator/manager later
        /// </summary>
        protected virtual int CalculatePoints(int steps) {
            return steps;
        }

        /// <summary>
        /// Check if we've crossed into a new day and reset daily counters
        /// </summary>
        private void CheckDailyReset() {
            DateTime today = DateTime.UtcNow.Date;

            if (LastStepDate.Date >= today) 
                return;
            
            Debug.Log($"[StepDataPersistence] New day detected. Yesterday's steps: {StepsToday}");
            StepsToday = 0;
            LastStepDate = today;
        }

        #endregion

        #region Helper Methods

        private void ResetToDefaults() {
            LastHealthSyncTimestamp = DateTime.UtcNow.AddDays(-1);
            TotalPointsAwarded = 0;
            PendingPedometerSteps = 0;
            LastActiveSource = StepSource.None;
            TotalStepsAllTime = 0;
            StepsToday = 0;
            LastStepDate = DateTime.MinValue;
        }

        #endregion

        #region Save Data Container

        [Serializable]
        private class SaveDataContainer {
            public long lastHealthSyncTicks;
            public int totalPointsAwarded;
            public int pendingPedometerSteps;
            public int lastActiveSource;
            public long totalStepsAllTime;
            public int stepsToday;
            public long lastStepDateTicks;
            public int version;
        }

        #endregion
    }
}