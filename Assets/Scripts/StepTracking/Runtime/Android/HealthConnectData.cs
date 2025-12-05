using System;

namespace GimGim.StepTracking.StepTracking {
    /// <summary>
    /// Availability status for Health Connect
    /// </summary>
    public enum HealthConnectAvailability {
        Unavailable = 0,
        Available = 1,
        NeedsUpdate = 2
    }
    
    /// <summary>
    /// Result from a Health Connect step query
    /// </summary>
    [Serializable]
    public class HealthConnectStepResult {
        public bool success;
        public long steps;
        public long startTime;
        public long endTime;
        public string source;
        public string errorCode;
        public string errorMessage;
    }
    
    /// <summary>
    /// Individual step record from Health Connect
    /// </summary>
    [Serializable]
    public class HealthConnectStepRecord {
        public long count;
        public long startTime;
        public long endTime;
        public string dataOrigin;
    }
}