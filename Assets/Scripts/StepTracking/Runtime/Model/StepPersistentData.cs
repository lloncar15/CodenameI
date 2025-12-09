using System;
using GimGim.Persistence;
using UnityEngine;

namespace GimGim.StepTracking {
    /// <summary>
    /// Serializable data container for step tracking data
    /// </summary>
    [Serializable]
    public class StepsData {
        public long lastHealthSyncTicks = DateTime.UtcNow.AddDays(-1).Ticks;
        public int pendingPedometerSteps = 0;
        public int lastActiveSource = 0;
        public long totalStepsAllTime = 0;
        public int stepsToday = 0;
        public long lastStepDateTicks = DateTime.MinValue.Ticks;
        public int version = 1;
    }
    
    /// <summary>
    /// Persistent data class for step tracking.
    /// Handles serialization and provides typed access to step data.
    /// </summary>
    [Serializable]
    public class StepPersistentData : PersistentDataBase<StepsData> {
        #region Properties

        /// <summary>
        /// Last time we synced steps from a health API
        /// </summary>
        public DateTime LastHealthSyncTimestamp {
            get => new(Data.lastHealthSyncTicks);
            set {
                Data.lastHealthSyncTicks = value.Ticks;
                MarkDirty();
            }
        }

        /// <summary>
        /// Steps accumulated from pedometer that haven't been "claimed" yet
        /// </summary>
        public int PendingPedometerSteps {
            get => Data.pendingPedometerSteps;
            set {
                Data.pendingPedometerSteps = value;
                MarkDirty();
            }
        }

        /// <summary>
        /// The step source that was last used
        /// </summary>
        public StepSource LastActiveSource {
            get => (StepSource)Data.lastActiveSource;
            set {
                Data.lastActiveSource = (int)value;
                MarkDirty();
            }
        }

        /// <summary>
        /// Total steps ever counted
        /// </summary>
        public long TotalStepsAllTime {
            get => Data.totalStepsAllTime;
            set {
                Data.totalStepsAllTime = value;
                MarkDirty();
            }
        }

        /// <summary>
        /// Steps counted today
        /// </summary>
        public int StepsToday {
            get => Data.stepsToday;
            set {
                Data.stepsToday = value;
                MarkDirty();
            }
        }

        /// <summary>
        /// The date of the last step count (for daily reset)
        /// </summary>
        public DateTime LastStepDate {
            get => new(Data.lastStepDateTicks);
            set {
                Data.lastStepDateTicks = value.Ticks;
                MarkDirty();
            }
        }

        #endregion
        
        #region Overrides
        
        protected override void OnLoaded() {
            CheckDailyReset();
        }
        
        /// <summary>
        /// Check if we've crossed into a new day and reset daily counters
        /// </summary>
        public void CheckDailyReset() {
            DateTime today = DateTime.UtcNow.Date;

            if (LastStepDate.Date >= today)
                return;

            Debug.Log($"[StepPersistentData] New day detected. Yesterday's steps: {StepsToday}");
            Data.stepsToday = 0;
            Data.lastStepDateTicks = today.Ticks;
            MarkDirty();
        }

        #endregion
    }
}