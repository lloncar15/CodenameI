using System;

namespace GimGim.Persistence {
    public interface IDailyResettable {
        /// <summary>
        /// Checks if crossed into a new day and calls OnDailyReset
        /// </summary>
        public void CheckDailyReset();

        /// <summary>
        /// Called when the daily reset happens
        /// </summary>
        public void OnDailyReset();
    }
}