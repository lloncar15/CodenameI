using System;
using GimGim.Persistence;

namespace GimGim.DailyReset {
    /// <summary>
    /// Serializable data container for daily reset
    /// </summary>
    [Serializable]
    public class DailyResetData {
        public DateTime LastResetTime = DateTime.MinValue;
    }
    
    public class DailyResetPersistentData : PersistentDataBase<DailyResetData> {
        #region Properties

        /// <summary>
        /// The last time a daily reset was performed
        /// </summary>
        public DateTime LastResetTime {
            get => Data.LastResetTime;
            set {
                Data.LastResetTime = value;
                MarkDirty();
            }
        }

        #endregion
    }
}