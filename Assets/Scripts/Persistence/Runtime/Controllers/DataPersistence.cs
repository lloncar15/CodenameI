using System;
using UnityEngine;

namespace GimGim.Persistence {
    public abstract class DataPersistence : IDataPersistence {

        private const string PREFS_KEY = nameof(IDataPersistence);
        
        public void Save() {
        }

        public void Load() {
        }

        public void ClearAll() {
            PlayerPrefs.DeleteKey(PREFS_KEY);
            PlayerPrefs.Save();
            ResetToDefaults();
            Debug.Log($"[{nameof(IDataPersistence)} All data cleared.]");
        }

        public virtual void ResetToDefaults() {
            // no-op
        }

        public virtual void ResetDailyData() {
            // no-op
        }
    }
    
    
    /// <summary>
    /// Abstract class used as a container for serialization
    /// </summary>
    [Serializable]
    public abstract class SaveDataContainer {
        public int version;
    }
}