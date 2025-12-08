namespace GimGim.Persistence {
    /// <summary>
    /// Interface for any class that handles persistence to PlayerPrefs
    /// </summary>
    public interface IDataPersistence {
        public void Save();
        public void Load();
        public void ClearAll();
        public void ResetToDefaults();
        public void ResetDailyData();
    }
}