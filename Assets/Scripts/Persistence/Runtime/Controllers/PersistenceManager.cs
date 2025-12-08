using System.Collections.Generic;
using UnityEngine;

namespace GimGim.Persistence {
    public class PersistenceManager : MonoBehaviour {
        #region Singleton

        private static PersistenceManager _instance;
        
        /// <summary>
        /// Singleton instance of the PersistenceManager
        /// </summary>
        public static PersistenceManager Instance {
            get {
                EnsureInitialized();
                return _instance;
            }
        }
        
        #endregion
        
        #region Static Fields

        private static readonly List<IPersistentData> _pendingRegistrations = new();
        private static bool _isInitialized;

        #endregion

        #region Instance Fields

        private readonly List<IPersistentData> _registeredData = new();
        private bool _hasLoadedAll;

        #endregion

        #region Properties

        /// <summary>
        /// Whether all persistent data has been loaded
        /// </summary>
        public bool HasLoadedAll => _hasLoadedAll;

        #endregion
        
        #region Initialization
        
        /// <summary>
        /// Creates the PersistenceManager before any scene loads
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Initialize() {
            EnsureInitialized();
        }

        /// <summary>
        /// Checks if the instance of the class exists
        /// and processes any registrations that happened before initialization.
        /// </summary>
        private static void EnsureInitialized() {
            if (_isInitialized) 
                return;

            GameObject go = new("PersistenceManager");
            _instance = go.AddComponent<PersistenceManager>();
            DontDestroyOnLoad(go);
            _isInitialized = true;
            
            foreach (IPersistentData data in _pendingRegistrations) {
                _instance._registeredData.Add(data);
            }
            _pendingRegistrations.Clear();
            _instance.LoadAll();
            
            Debug.Log("[PersistenceManager] Initialized");
        }

        
        #endregion
        
        #region Registration

        /// <summary>
        /// Registers a persistent data instance with the manager.
        /// Called automatically by PersistentDataBase constructor.
        /// </summary>
        /// <param name="data">The persistent data instance to register</param>
        public static void Register(IPersistentData data) {
            if (data == null) 
                return;

            if (_isInitialized && _instance != null) {
                if (_instance._registeredData.Contains(data)) 
                    return;
                _instance._registeredData.Add(data);
                
                if (_instance._hasLoadedAll) {
                    data.Load();
                }
                    
                Debug.Log($"[PersistenceManager] Registered: {data.Key}");
            }
            else {
                if (!_pendingRegistrations.Contains(data)) {
                    _pendingRegistrations.Add(data);
                }
            }
        }

        /// <summary>
        /// Unregisters a persistent data instance from the manager
        /// </summary>
        /// <param name="data">The persistent data instance to unregister</param>
        public static void Unregister(IPersistentData data) {
            if (data == null) 
                return;

            if (_isInitialized && _instance != null) {
                _instance._registeredData.Remove(data);
                Debug.Log($"[PersistenceManager] Unregistered: {data.Key}");
            }
            else {
                _pendingRegistrations.Remove(data);
            }
        }

        #endregion

        #region Save/Load Operations

        /// <summary>
        /// Loads all registered persistent data
        /// </summary>
        public void LoadAll() {
            Debug.Log($"[PersistenceManager] Loading all ({_registeredData.Count} registered)...");
            
            foreach (IPersistentData data in _registeredData) {
                data.Load();
            }
            
            _hasLoadedAll = true;
            Debug.Log("[PersistenceManager] LoadAll complete");
        }

        /// <summary>
        /// Saves all registered persistent data that has unsaved changes
        /// </summary>
        public void SaveAll() {
            Debug.Log($"[PersistenceManager] Saving all ({_registeredData.Count} registered)...");
            
            foreach (IPersistentData data in _registeredData) {
                data.Save();
            }
            
            Debug.Log("[PersistenceManager] SaveAll complete");
        }

        /// <summary>
        /// Forces save of all registered persistent data, regardless of dirty state
        /// </summary>
        public void ForceSaveAll() {
            Debug.Log($"[PersistenceManager] Force saving all ({_registeredData.Count} registered)...");
            
            foreach (IPersistentData data in _registeredData) {
                data.MarkDirty();
                data.Save();
            }
            
            Debug.Log("[PersistenceManager] ForceSaveAll complete");
        }

        /// <summary>
        /// Saves a specific persistent data instance by key
        /// </summary>
        /// <param name="key">The key of the persistent data to save</param>
        /// <returns>True if found and saved, false otherwise</returns>
        public bool Save(string key) {
            IPersistentData data = _registeredData.Find(d => d.Key == key);
            
            if (data != null) {
                data.Save();
                return true;
            }
            
            Debug.LogWarning($"[PersistenceManager] No persistent data found with key '{key}'");
            return false;
        }

        /// <summary>
        /// Clears all registered persistent data
        /// </summary>
        public void ClearAll() {
            Debug.Log("[PersistenceManager] Clearing all persistent data...");
            
            foreach (IPersistentData data in _registeredData) {
                data.Clear();
            }
            
            Debug.Log("[PersistenceManager] ClearAll complete");
        }

        #endregion
        
        #region Unity Lifecycle

        private void OnApplicationPause(bool pauseStatus) {
            if (!pauseStatus) 
                return;
            
            Debug.Log("[PersistenceManager] Application pausing - saving all data");
            SaveAll();
        }

        private void OnApplicationQuit() {
            Debug.Log("[PersistenceManager] Application quitting - saving all data");
            SaveAll();
        }

        private void OnDestroy() {
            if (_instance == this) {
                _instance = null;
                _isInitialized = false;
            }
        }

        #endregion
        
        #region Debug

        /// <summary>
        /// Gets debug information about all registered persistent data
        /// </summary>
        /// <returns>Debug string with registration info</returns>
        public string GetDebugInfo() {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            sb.AppendLine($"PersistenceManager - {_registeredData.Count} registered:");
            
            foreach (IPersistentData data in _registeredData) {
                sb.AppendLine($"  [{data.Key}] Dirty: {data.IsDirty}");
            }
            
            return sb.ToString();
        }

        #endregion
    }
}