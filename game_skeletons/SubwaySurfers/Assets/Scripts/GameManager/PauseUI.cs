using SubwaySurfers.Tutorial.Core;
using SubwaySurfers.Tutorial.Events;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using ReusablePatterns.SharedCore.Runtime.PauseSystem;

namespace SubwaySurfers
{
    /// <summary>
    /// Handles UI elements for the pause system, listening to PauseManager events
    /// </summary>
    public class PauseUI : MonoBehaviour
    {
        [Header("UI Elements")] public RectTransform pauseMenu;
        public RectTransform wholeUI;
        public Button pauseButton;
        public Button resumeButton;
        public TMP_Text countdownText;

        private RectTransform m_CountdownRectTransform;
        private IGamePauser m_PauseManager;

        private void Awake()
        {
            m_CountdownRectTransform = countdownText?.GetComponent<RectTransform>();

            // Find references if not assigned
            m_PauseManager = FindFirstObjectByType<PauseManager>(FindObjectsInactive.Include);

            if (m_PauseManager == null)
            {
                Debug.LogError("PauseUI requires a PauseManager in the scene!");
                return;
            }

            SetupEvents();
            SetupButtons();
        }

        private void SetupEvents()
        {
            // Subscribe to PauseManager events
            m_PauseManager.OnPaused += HandlePaused;
            m_PauseManager.OnResumed += HandleResumed;
            m_PauseManager.OnResumeStarted += HandleResumeStarted;
            m_PauseManager.OnCountdownUpdated += HandleCountdownUpdated;
            m_PauseManager.OnCountdownFinished += HandleCountdownFinished;
            TutorialEventBus.OnTutorialStart += OnTutorialStarted;
            TutorialEventBus.OnTutorialCompleted += OnTutorialCompleted;
        }

        private void SetupButtons()
        {
            if (pauseButton != null)
            {
                pauseButton.onClick.AddListener(() => m_PauseManager.Pause(PauseData.Default));
            }

            if (resumeButton != null)
            {
                resumeButton.onClick.AddListener(() => m_PauseManager.Resume());
            }
        }

        private void OnTutorialStarted(TutorialStartEvent data)
        {
            UpdateButtonVisibility();
        }

        private void OnTutorialCompleted(TutorialCompletedEvent data)
        {
            UpdateButtonVisibility();
        }

        private void UpdateButtonVisibility()
        {
            if (pauseButton != null)
            {
                pauseButton.gameObject.SetActive(
                    (m_PauseManager == null || m_PauseManager.IsPaused == false) &&
                    (ITutorialManager.Instance == null || ITutorialManager.Instance.IsActive == false));
            }
        }

        private void HandlePaused(PauseData data)
        {
            HandlePauseStateChange(data, true);
        }

        private void HandleResumed(PauseData data)
        {
            HandlePauseStateChange(data, false);
        }

        private void HandleResumeStarted(PauseData data)
        {
            // Show countdown UI
            if (countdownText != null)
            {
                countdownText.gameObject.SetActive(data.resumeWithCountdown);
            }

            HandlePauseStateChange(data, false);
        }

        private void HandleCountdownUpdated(PauseData data, float countdownValue)
        {
            if (countdownText == null || m_CountdownRectTransform == null || data.resumeWithCountdown == false)
                return;

            countdownText.text = Mathf.Ceil(countdownValue).ToString();

            // Calculate scale based on the fraction within the current second
            float scaleFactor = 1.0f - (countdownValue - Mathf.Floor(countdownValue));
            scaleFactor = Mathf.Clamp(scaleFactor, 0.5f, 1.5f); // Prevent too small/large sizes
            m_CountdownRectTransform.localScale = Vector3.one * scaleFactor;
        }

        private void HandleCountdownFinished(PauseData data)
        {
            // Hide countdown UI
            if (countdownText != null)
            {
                countdownText.gameObject.SetActive(false);

                if (m_CountdownRectTransform != null)
                    m_CountdownRectTransform.localScale = Vector3.zero;
            }
        }

        private void HandlePauseStateChange(PauseData data, bool isPaused)
        {
            if (pauseMenu != null)
                pauseMenu.gameObject.SetActive(isPaused && data.displayMenu);

            if (wholeUI != null)
                wholeUI.gameObject.SetActive(isPaused == false || data.displayMenu == false);
            UpdateButtonVisibility();
        }

        private void OnDestroy()
        {
            // Unsubscribe from all events
            if (m_PauseManager != null)
            {
                m_PauseManager.OnPaused -= HandlePaused;
                m_PauseManager.OnResumed -= HandleResumed;
                m_PauseManager.OnResumeStarted -= HandleResumeStarted;
                m_PauseManager.OnCountdownUpdated -= HandleCountdownUpdated;
                m_PauseManager.OnCountdownFinished -= HandleCountdownFinished;
            }
        }
    }
}