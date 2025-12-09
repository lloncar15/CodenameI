using System;

namespace GimGim.StepTracking {
    /// <summary>
    /// Data container for step query results
    /// </summary>
    [Serializable]
    public struct StepQueryData {
        public bool success;
        public int steps;
        public string error;
        public DateTime FromDate;
        public DateTime ToDate;
        public StepSource source;

        public static StepQueryData Failed(string errorMessage, StepSource fromSource = StepSource.None) {
            return new StepQueryData {
                success = false,
                steps = 0,
                error = errorMessage,
                FromDate = DateTime.MinValue,
                ToDate = DateTime.MinValue,
                source = fromSource
            };
        }

        public static StepQueryData Succeeded(int steps, DateTime from, DateTime to, StepSource fromSource) {
            return new StepQueryData {
                success = true,
                steps = steps,
                error = string.Empty,
                FromDate = from,
                ToDate = to,
                source = fromSource
            };
        }
    }
}