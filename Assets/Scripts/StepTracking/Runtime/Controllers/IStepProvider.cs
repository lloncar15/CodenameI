using System;

namespace GimGim.StepTracking {
    /// <summary>
    /// Interface for all step tracking providers (Health APIs and pedometers)
    /// </summary>
    public interface IStepProvider {
        /// <summary>
        /// The type of step source this provider represents
        /// </summary>
        StepSource Source { get; }
        
        /// <summary>
        /// Whether this provider is available on the current device
        /// </summary>
        bool IsAvailable { get; }

        /// <summary>
        /// Whether the user has authorized this provider to access step data
        /// </summary>
        bool IsAuthorized { get; }

        /// <summary>
        /// Whether this provider supports real-time step tracking
        /// </summary>
        bool SupportsRealTime { get; }

        /// <summary>
        /// Whether this provider supports querying historical step data
        /// </summary>
        bool SupportsHistoricalData { get; }

        /// <summary>
        /// Request authorization from the user to access step data
        /// </summary>
        /// <param name="callback">Called with true if authorized, false otherwise</param>
        void RequestAuthorization(Action<bool> callback);

        /// <summary>
        /// Get the total steps taken since a specific time
        /// Only works if SupportsHistoricalData is true
        /// </summary>
        /// <param name="since">Start time for the query</param>
        /// <param name="callback">Called with the step data result</param>
        void GetStepsSince(DateTime since, Action<StepData> callback);

        /// <summary>
        /// Event fired when steps are detected in real-time
        /// Only fires if SupportsRealTime is true and tracking is started
        /// </summary>
        event Action<int> OnStepsUpdated;

        /// <summary>
        /// Start real-time step tracking (foreground only)
        /// </summary>
        void StartRealTimeTracking();

        /// <summary>
        /// Stop real-time step tracking
        /// </summary>
        void StopRealTimeTracking();
    }
}