using System;
using GimGim.Persistence;

namespace GimGim.Points {
    /// <summary>
    /// Serializable data container for points data
    /// </summary>
    [Serializable]
    public class PointsData {
        public int currentPoints;
        public int allTimePoints;
    }
    
    public class PointsPersistentData : PersistentDataBase<PointsData> {
        #region Properties

        /// <summary>
        /// Number of points we currently have
        /// </summary>
        public int CurrentPoints {
            get => Data.currentPoints;
            set {
                Data.currentPoints = value;
                MarkDirty();
            }
        }

        /// <summary>
        /// Number of points we accumulated over the app lifetime
        /// </summary>
        public int AllTimePoints {
            get => Data.allTimePoints;
            set {
                Data.allTimePoints = value;
                MarkDirty();
            }
        }

        #endregion

        #region Overrides

        protected override void OnLoaded() {
            CheckDailyReset();
        }

        
        public void CheckDailyReset() {
            
        }

        public void OnDailyReset() {
            
        }

        #endregion
    }
}