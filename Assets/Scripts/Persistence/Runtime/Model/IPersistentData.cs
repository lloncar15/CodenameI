namespace GimGim.Persistence {
    /// <summary>
    /// Generic interface for persistent data classes.
    /// T is the serializable data container type.
    /// </summary>
    /// <typeparam name="T">The serializable data container type</typeparam>
    public interface IPersistentData<T> : IPersistentData where T : class, new() {
        /// <summary>
        /// The underlying data container
        /// </summary>
        T Data { get; }
    }
    
    /// <summary>
    /// Non-generic base interface for persistence manager to work with
    /// </summary>
    public interface IPersistentData {
        /// <summary>
        /// Unique key for PlayerPrefs storage
        /// </summary>
        string Key { get; }
        
        /// <summary>
        /// Whether this data has unsaved changes
        /// </summary>
        bool IsDirty { get; }
        
        /// <summary>
        /// Save data to persistent storage
        /// </summary>
        void Save();
        
        /// <summary>
        /// Load data from persistent storage
        /// </summary>
        void Load();
        
        /// <summary>
        /// Clear all saved data and reset to defaults
        /// </summary>
        void Clear();
        
        /// <summary>
        /// Mark data as having unsaved changes
        /// </summary>
        void MarkDirty();
    }
}