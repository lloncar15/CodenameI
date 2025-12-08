using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace GimGim.StepTracking
{
    /// <summary>
    /// Simple test UI for the step tracking system.
    /// Attach this to a Canvas and assign the UI elements.
    /// 
    /// This is meant for testing - replace with your actual game UI.
    /// </summary>
    public class StepTrackingTestUI : MonoBehaviour
    {
        #region Inspector Fields
        
        [Header("Text Displays")]
        [SerializeField] private TextMeshProUGUI _statusText;
        [SerializeField] private TextMeshProUGUI _stepsText;
        [SerializeField] private TextMeshProUGUI _pointsText;
        [SerializeField] private TextMeshProUGUI _debugText;
        
        [Header("Buttons")]
        [SerializeField] private Button _toggleTrackingButton;
        [SerializeField] private Button _addTestStepsButton;
        [SerializeField] private Button _clearDataButton;
        
        [Header("Settings")]
        [SerializeField] private int _testStepsToAdd = 100;
        [SerializeField] private float _updateInterval = 0.5f;
        
        #endregion

        #region Private Fields
        
        private float _lastUpdateTime;
        private TextMeshProUGUI _toggleButtonText;
        
        #endregion

        #region Unity Lifecycle
        
        private void Start()
        {
            // Get button text component
            if (_toggleTrackingButton != null)
            {
                _toggleButtonText = _toggleTrackingButton.GetComponentInChildren<TextMeshProUGUI>();
            }
            
            // Setup button listeners
            if (_toggleTrackingButton != null)
            {
                _toggleTrackingButton.onClick.AddListener(OnToggleTrackingClicked);
            }
            
            if (_addTestStepsButton != null)
            {
                _addTestStepsButton.onClick.AddListener(OnAddTestStepsClicked);
            }
            
            if (_clearDataButton != null)
            {
                _clearDataButton.onClick.AddListener(OnClearDataClicked);
            }
            
            if (StepController.Instance != null)
            {
                StepController.Instance.OnStepsDetected += OnStepsDetected;
                StepController.Instance.OnPointsAwarded += OnPointsAwarded;
                StepController.Instance.OnInitialized += OnManagerInitialized;
            }
            
            // Initial UI update
            UpdateUI();
        }
        
        private void OnDestroy()
        {
            if (StepController.Instance != null)
            {
                StepController.Instance.OnStepsDetected -= OnStepsDetected;
                StepController.Instance.OnPointsAwarded -= OnPointsAwarded;
                StepController.Instance.OnInitialized -= OnManagerInitialized;
            }
        }
        
        private void Update()
        {
            // Periodic UI update
            if (Time.time - _lastUpdateTime > _updateInterval)
            {
                _lastUpdateTime = Time.time;
                UpdateUI();
            }
        }
        
        #endregion

        #region UI Updates
        
        private void UpdateUI()
        {
            var manager = StepController.Instance;
            
            if (manager == null)
            {
                SetStatusText("StepDataManager not found!", Color.red);
                return;
            }
            
            // Status
            if (!manager.IsInitialized)
            {
                SetStatusText("Initializing...", Color.yellow);
            }
            else if (manager.ActiveSource == StepSource.None)
            {
                SetStatusText("No step source available", Color.red);
            }
            else if (manager.IsTracking)
            {
                SetStatusText($"Tracking ({manager.ActiveSource})", Color.green);
            }
            else
            {
                SetStatusText($"Ready ({manager.ActiveSource})", Color.white);
            }
            
            // Steps
            if (_stepsText != null)
            {
                _stepsText.text = $"Steps Today: {manager.StepsToday:N0}\n" +
                                  $"Session: {manager.GetSessionSteps():N0}\n" +
                                  $"All Time: {manager.TotalStepsAllTime:N0}";
            }
            
            // Points
            if (_pointsText != null)
            {
                // _pointsText.text = $"Points: {manager.TotalPoints:N0}";
            }
            
            // Debug info
            if (_debugText != null)
            {
                _debugText.text = manager.GetDebugStatus();
            }
            
            // Toggle button text
            if (_toggleButtonText != null)
            {
                _toggleButtonText.text = manager.IsTracking ? "Stop Tracking" : "Start Tracking";
            }
        }
        
        private void SetStatusText(string text, Color color)
        {
            if (_statusText != null)
            {
                _statusText.text = text;
                _statusText.color = color;
            }
        }
        
        #endregion

        #region Button Handlers
        
        private void OnToggleTrackingClicked()
        {
            var manager = StepController.Instance;
            if (manager == null) return;
            
            manager.ToggleTracking();
            UpdateUI();
        }
        
        private void OnAddTestStepsClicked()
        {
            var manager = StepController.Instance;
            if (manager == null) return;
            
            int points = manager.AddStepsManually(_testStepsToAdd);
            Debug.Log($"Added {_testStepsToAdd} test steps, earned {points} points");
            UpdateUI();
        }
        
        private void OnClearDataClicked()
        {
            var manager = StepController.Instance;
            if (manager == null) return;
            
            manager.ClearAllData();
            UpdateUI();
        }
        
        #endregion

        #region Event Handlers
        
        private void OnStepsDetected(int steps)
        {
            Debug.Log($"[TestUI] Steps detected: {steps}");
            UpdateUI();
        }
        
        private void OnPointsAwarded(int points)
        {
            Debug.Log($"[TestUI] Points awarded: {points}");
            
            // You could show a popup or animation here
            UpdateUI();
        }
        
        private void OnManagerInitialized(bool success)
        {
            Debug.Log($"[TestUI] Manager initialized: {success}");
            UpdateUI();
        }
        
        #endregion
    }
}
