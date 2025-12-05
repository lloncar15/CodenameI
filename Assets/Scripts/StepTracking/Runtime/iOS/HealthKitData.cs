using System;

namespace GimGim.StepTracking.StepTracking {
    /// <summary>
    /// Result from a HealthKit step query
    /// </summary>
    [Serializable]
    public class HealthKitStepResult {
        public bool success;
        public long steps;
        public long startTime;
        public long endTime;
        public string source;
        public string errorCode;
        public string errorMessage;
    }
}