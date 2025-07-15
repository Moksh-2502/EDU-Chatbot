using UnityEngine;
using UnityEngine.UI;
using SubwaySurfers.Tutorial.Events;
using SubwaySurfers.Tutorial.Core;
using SubwaySurfers.Tutorial.Data;
using TMPro;

namespace SubwaySurfers.Tutorial.UI
{
    public class TutorialUIController : MonoBehaviour, ITutorialUIController
    {
        [Header("UI References")]
        [SerializeField] private GameObject tutorialUIPanel;
        [SerializeField] private Transform stepUIRoot; // Root transform for step-specific UI objects
        [SerializeField] private TMP_Text instructionText, progressText;
        [SerializeField] private GameObject completionPanel;
        [SerializeField] private Button skipButton;
        [SerializeField] private Button restartButton;

        [Header("Settings")]
        [SerializeField] private TutorialConfig tutorialConfig = null;

        private ITutorialManager _tutorialManager;
        private bool _isVisible = false;
        private GameObject _currentStepUIObject; // Currently instantiated step UI object

        private void Awake()
        {
            _tutorialManager = FindFirstObjectByType<TutorialManager>(FindObjectsInactive.Include);
            Initialize();
        }

        private void OnEnable()
        {
            SubscribeToEvents();
        }

        private void OnDisable()
        {
            UnsubscribeFromEvents();
        }

        public void Initialize()
        {
            // Setup UI references
            if (tutorialUIPanel != null)
                tutorialUIPanel.SetActive(false);
            
            if (completionPanel != null)
                completionPanel.SetActive(false);

            if (skipButton != null)
            {
                skipButton.gameObject.SetActive(tutorialConfig.AllowSkipping);
                skipButton.onClick.AddListener(OnSkipButtonClicked);
            }

            if (restartButton != null)
                restartButton.onClick.AddListener(OnRestartButtonClicked);

            Debug.Log("TutorialUIController: Initialized");
        }

        private void SubscribeToEvents()
        {
            TutorialEventBus.OnUIEvent += HandleUIEvent;
            TutorialEventBus.OnProgressChanged += HandleProgressChanged;
            TutorialEventBus.OnStateChanged += HandleStateChanged;
            TutorialEventBus.OnTutorialCompleted += HandleTutorialCompleted;
            TutorialEventBus.OnStepStarted += HandleStepStarted;
            TutorialEventBus.OnStepCompleted += HandleStepCompleted;
        }

        private void UnsubscribeFromEvents()
        {
            TutorialEventBus.OnUIEvent -= HandleUIEvent;
            TutorialEventBus.OnProgressChanged -= HandleProgressChanged;
            TutorialEventBus.OnStateChanged -= HandleStateChanged;
            TutorialEventBus.OnTutorialCompleted -= HandleTutorialCompleted;
            TutorialEventBus.OnStepStarted -= HandleStepStarted;
            TutorialEventBus.OnStepCompleted -= HandleStepCompleted;
        }

        public void HandleUIEvent(TutorialUIEvent uiEvent)
        {
            switch (uiEvent.EventType)
            {
                case TutorialUIEvent.UIEventType.ShowInstructions:
                    ShowInstructions(uiEvent.Message);
                    break;
                case TutorialUIEvent.UIEventType.HideInstructions:
                    HideInstructions();
                    break;
                case TutorialUIEvent.UIEventType.UpdateProgress:
                    // Progress updates are handled by HandleProgressChanged
                    break;
            }
        }

        public void ShowTutorialUI()
        {
            if (tutorialUIPanel != null && !_isVisible)
            {
                tutorialUIPanel.SetActive(true);
                _isVisible = true;
                Debug.Log("TutorialUIController: Tutorial UI shown");
            }
        }

        public void HideTutorialUI()
        {
            if (tutorialUIPanel != null && _isVisible)
            {
                tutorialUIPanel.SetActive(false);
                _isVisible = false;
                Debug.Log("TutorialUIController: Tutorial UI hidden");
            }
        }

        public void ShowCompletionUI()
        {
            if (completionPanel != null)
            {
                completionPanel.SetActive(true);
            }
        }

        private void ShowInstructions(string instructions)
        {
            ShowTutorialUI();
            
            if (instructionText != null)
            {
                instructionText.text = instructions;
                Debug.Log($"TutorialUIController: Instructions updated: {instructions}");
            }
        }

        private void HideInstructions()
        {
            if (instructionText != null)
            {
                instructionText.text = "";
            }
        }

        private void HandleProgressChanged(TutorialProgressEvent progressEvent)
        {
            // Update progress text
            if (progressText != null)
            {
                progressText.text = $"{progressEvent.SuccessfulActions}/{progressEvent.RequiredActions}";
            }

            Debug.Log($"TutorialUIController: Progress updated - {progressEvent.SuccessfulActions}/{progressEvent.RequiredActions} ({progressEvent.CompletionPercentage:P0})");
        }

        private void HandleStateChanged(TutorialStateChangedEvent stateEvent)
        {
            if (stateEvent.IsActive)
            {
                ShowTutorialUI();
                
                // Update skip button visibility based on config
                if (skipButton != null && tutorialConfig != null)
                {
                    bool canSkip = tutorialConfig.AllowSkipping;
                    skipButton.gameObject.SetActive(canSkip);
                }
            }
            else
            {
                // Clean up current step UI object when tutorial becomes inactive
                DestroyCurrentStepUIObject();
                HideTutorialUI();
            }

            Debug.Log($"TutorialUIController: State changed - Active: {stateEvent.IsActive}, Paused: {stateEvent.IsPaused}");
        }

        private void HandleTutorialCompleted(TutorialCompletedEvent completionEvent)
        {
            // Clean up current step UI object
            DestroyCurrentStepUIObject();
            
            HideTutorialUI();
            
            if (completionEvent.Success)
            {
                ShowCompletionUI();
            }
            
            Debug.Log($"TutorialUIController: Tutorial completed - Success: {completionEvent.Success}");
        }

        private void OnSkipButtonClicked()
        {
            Debug.Log("TutorialUIController: Skip button clicked");
            _tutorialManager?.SkipCurrentStep();
        }

        private void OnRestartButtonClicked()
        {
            Debug.Log("TutorialUIController: Restart button clicked");
            
            if (completionPanel != null)
                completionPanel.SetActive(false);
                
            _tutorialManager?.RestartTutorial();
        }

        // Public methods for external UI control
        public void SetInstructionText(string text)
        {
            if (instructionText != null)
                instructionText.text = text;
        }

        public void SetProgressText(string text)
        {
            if (progressText != null)
                progressText.text = text;
        }

        private void HandleStepStarted(TutorialStepStartedEvent stepEvent)
        {
            // Destroy the previous step UI object if it exists
            DestroyCurrentStepUIObject();

            // Get the step data to access the uiObject prefab
            var stepData = tutorialConfig?.GetStepData(stepEvent.StepType);
            if (stepData?.uiObject != null && stepUIRoot != null)
            {
                // Instantiate the step-specific UI object
                _currentStepUIObject = Instantiate(stepData.uiObject, stepUIRoot);
                Debug.Log($"TutorialUIController: Instantiated UI object for step: {stepEvent.StepName}");
            }
        }

        private void HandleStepCompleted(TutorialStepCompletedEvent stepEvent)
        {
            
            
            Debug.Log($"TutorialUIController: Step completed: {stepEvent.StepName} - Instructions and UI objects cleared");
        }

        private void DestroyCurrentStepUIObject()
        {
            if (_currentStepUIObject != null)
            {
                DestroyImmediate(_currentStepUIObject);
                _currentStepUIObject = null;
                Debug.Log("TutorialUIController: Destroyed current step UI object");
            }
        }
    }
} 