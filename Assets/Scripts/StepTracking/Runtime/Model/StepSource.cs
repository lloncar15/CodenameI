namespace GimGim.StepTracking {
    public enum StepSource {
        None,
        HealthKit,                  // iOS Health API
        HealthConnect,              // Android Health Connect API
        InputSystemPedometer,       // Unity Input System StepCounter
        NatStep                     // Fallback pedometer library
    }
}