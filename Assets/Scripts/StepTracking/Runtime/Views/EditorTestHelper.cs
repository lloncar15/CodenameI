using UnityEngine;

namespace GimGim.StepTracking {
    public class EditorTestHelper : MonoBehaviour
    {
        void Start()
        {
            var manager = StepDataManager.Instance;
            manager.OnPointsAwarded += (pts) => Debug.Log($"Points: {pts}");
            manager.OnStepsDetected += (steps) => Debug.Log($"Steps: {steps}");
        }
    
        void Update()
        {
            // Press Space to simulate 100 steps
            if (Input.GetKeyDown(KeyCode.Space))
            {
                StepDataManager.Instance.AddStepsManually(100);
            }
        
            // Press R to reset all data
            if (Input.GetKeyDown(KeyCode.R))
            {
                StepDataManager.Instance.ClearAllData();
            }
        }
    }
}