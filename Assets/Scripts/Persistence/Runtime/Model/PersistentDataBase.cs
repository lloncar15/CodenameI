using System;
using UnityEngine;

namespace GimGim.Persistence {
    /// <summary>
    /// Abstract base class for any persistent data that auto-registers with PersistenceManager in the constructor
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public abstract class PersistentDataBase<T> : IPersistentData<T> where T : class, new() {
        #region Fields

        private T _data;
        private bool _isDirty;
        private bool _isLoaded;

        #endregion
        
        #region Properties
        
        public string Key => $"Persistence_{typeof(T).Name}";
        public bool IsDirty => _isDirty;
        
        public T Data {
            get {
                if (!_isLoaded) {
                    Debug.LogWarning($"[{GetType().Name}] Accessing data before Load() was called. Call PersistenceManager.LoadAll() first.");
                }
                return _data;
            }
        }
        
        #endregion

        #region Constructor

        /// <summary>
        /// Creates a new persistent data instance and registers with PersistenceManager
        /// </summary>
        protected PersistentDataBase() {
            _data = new T();
            _isDirty = false;
            _isLoaded = false;
            
            PersistenceManager.Register(this);
        }

        #endregion

        #region IPersitentData Implementation
        
        public void Save() {
            if (!_isDirty) {
                Debug.Log($"[{GetType().Name}] Skipping save - no changes");
                return;
            }

            try {
                string json = JsonUtility.ToJson(_data);
                PlayerPrefs.SetString(Key, json);
                PlayerPrefs.Save();
                _isDirty = false;
                
                Debug.Log($"[{GetType().Name}] Saved to key '{Key}'");
            }
            catch (Exception e) {
                Debug.LogError($"[{GetType().Name}] Failed to save: {e.Message}");
            }
        }
        
        public void Load() {
            try {
                if (!PlayerPrefs.HasKey(Key)) {
                    Debug.Log($"[{GetType().Name}] No saved data found at key '{Key}', using defaults");
                    _data = new T();
                    OnLoaded();
                    _isLoaded = true;
                    return;
                }

                string json = PlayerPrefs.GetString(Key);
                _data = JsonUtility.FromJson<T>(json);
                
                if (_data == null) {
                    Debug.LogWarning($"[{GetType().Name}] Failed to deserialize data, using defaults");
                    _data = new T();
                }

                OnLoaded();
                _isLoaded = true;
                _isDirty = false;
                
                Debug.Log($"[{GetType().Name}] Loaded from key '{Key}'");
            }
            catch (Exception e) {
                Debug.LogError($"[{GetType().Name}] Failed to load: {e.Message}");
                _data = new T();
                _isLoaded = true;
            }
        }
        
        public void Clear() {
            PlayerPrefs.DeleteKey(Key);
            PlayerPrefs.Save();
            _data = new T();
            _isDirty = false;
            _isLoaded = true;
            
            Debug.Log($"[{GetType().Name}] Cleared data at key '{Key}'");
        }
        
        public void MarkDirty() {
            _isDirty = true;
        }
        
        #endregion

        #region Virtual Methods

        /// <summary>
        /// Called after data is loaded. Override to perform post-load operations
        /// like data migration or validation.
        /// </summary>
        protected virtual void OnLoaded() { }

        #endregion
    }
}