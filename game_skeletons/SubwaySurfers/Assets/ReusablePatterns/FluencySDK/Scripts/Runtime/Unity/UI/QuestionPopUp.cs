using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.UI;
using DG.Tweening;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using FluencySDK.Unity;
using SharpShuffleBag;
using TMPro;

namespace FluencySDK.UI
{
    public class QuestionPopUp : BaseQuestionUI
    {
        [SerializeField] private RectTransform answerContainer;
        [SerializeField] private AnswerButton answerBtnPrefab;

        [Header("Timer Settings")] [SerializeField]
        private GameObject clockGameObject;

        [SerializeField] private Image timerFillImage;
        [SerializeField] private Color fullTimeColor = Color.green;
        [SerializeField] private Color mediumTimeColor = Color.yellow;
        [SerializeField] private Color lowTimeColor = Color.red;
        [SerializeField] private float mediumTimeThreshold = 0.35f; // 35% of time left
        [SerializeField] private float lowTimeThreshold = 0.10f; // 10% of time left 

        [Header("Mastery Mode Settings")] [SerializeField]
        private int retryCountdown = 3; // How long to show correct answer

        [SerializeField] private GameObject retryCountdownContainer;
        [SerializeField] private TMP_Text retryCountdownText;
        [SerializeField] private float retryObjectTweenDuration = 0.35f;

        private ObjectPool<AnswerButton> _btnsPool;
        private readonly List<AnswerButton> _answerButtons = new List<AnswerButton>();

        // Keyboard navigation fields
        private int _selectedButtonIndex = -1;
        private bool _keyboardNavigationEnabled = false;

        // Timer fields
        private Tween _timerFillTween;

        private IQuestionProvider _questionProvider;

        protected override void Initialize()
        {
            base.Initialize();

            _btnsPool = new ObjectPool<AnswerButton>(() =>
                {
                    var btn = Instantiate(answerBtnPrefab, answerContainer);
                    btn.transform.localScale = Vector3.one;
                    return btn;
                }, fetched => { fetched.gameObject.SetActive(true); },
                actionOnRelease: (released) => { released.gameObject.SetActive(false); },
                actionOnDestroy: (destroyed) => { Destroy(destroyed.gameObject); });

            // Initialize timer UI
            if (clockGameObject != null)
            {
                clockGameObject.SetActive(false);
            }

            if (timerFillImage != null)
            {
                timerFillImage.fillAmount = 1f;
                timerFillImage.color = fullTimeColor;
            }

            _questionProvider = BaseQuestionProvider.Instance;

            if (_questionProvider != null)
            {
                _questionProvider.OnQuestionAnswerSubmitAttempted += HandleAnswerSubmitAttempted;
            }
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            GameInputController.OnLeftInput += OnLeftInput;
            GameInputController.OnRightInput += OnRightInput;
            GameInputController.OnUpInput += OnUpInput;
            GameInputController.OnDownInput += OnDownInput;
            GameInputController.OnConfirmInput += OnConfirmInput;
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            GameInputController.OnLeftInput -= OnLeftInput;
            GameInputController.OnRightInput -= OnRightInput;
            GameInputController.OnUpInput -= OnUpInput;
            GameInputController.OnDownInput -= OnDownInput;
            GameInputController.OnConfirmInput -= OnConfirmInput;

            // Clean up timer animation
            _timerFillTween?.Kill();

            // Stop timer
            StopTimer();
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            // Ensure tweens are killed when object is destroyed
            _timerFillTween?.Kill();
            
            if (_questionProvider != null)
            {
                _questionProvider.OnQuestionAnswerSubmitAttempted -= HandleAnswerSubmitAttempted;
            }
        }

        // Event handlers for centralized input - translate to UI navigation
        private void OnLeftInput()
        {
            if (!ShouldHandleInput()) return;
            HandleNavigationInput(GetLeftButtonIndex(_selectedButtonIndex));
        }

        private void OnRightInput()
        {
            if (!ShouldHandleInput()) return;
            HandleNavigationInput(GetRightButtonIndex(_selectedButtonIndex));
        }

        private void OnUpInput()
        {
            if (!ShouldHandleInput()) return;
            HandleNavigationInput(GetUpButtonIndex(_selectedButtonIndex));
        }

        private void OnDownInput()
        {
            if (!ShouldHandleInput()) return;
            HandleNavigationInput(GetDownButtonIndex(_selectedButtonIndex));
        }

        private void HandleNavigationInput(int newIndex)
        {
            if (_selectedButtonIndex == -1)
            {
                // First navigation input - select button 0
                newIndex = 0;
            }

            if (newIndex != _selectedButtonIndex)
            {
                SetSelectedButton(newIndex);
            }
        }

        private void OnConfirmInput()
        {
            if (!ShouldHandleInput()) return;

            // Submit the selected answer
            if (_selectedButtonIndex >= 0 && _selectedButtonIndex < _answerButtons.Count)
            {
                _answerButtons[_selectedButtonIndex].SimulateClick();
            }
        }

        private bool ShouldHandleInput()
        {
            return _keyboardNavigationEnabled &&
                   questionPresentationType == QuestionPresentationType.FullScreen &&
                   _answerButtons.Count > 0;
        }

        // Mastery Mode Logic
        private void HandleAnswerSubmitAttempted(IQuestion question, SubmitAnswerResult result)
        {
            // Only handle if this is our current question
            if (_currentQuestion.Question == null || question?.Id != _currentQuestion.Question?.Id)
            {
                return;
            }

            _keyboardNavigationEnabled = false;
            
            if (result.ShouldRetry == false)
            {
                return;
            }

            // Wrong answer in mastery mode - start retry flow
            StartRetryFlowAsync(this.GetCancellationTokenOnDestroy()).Forget();
        }

        private async UniTask TweenRetryCountdown(CancellationToken cancellationToken)
        {
            if (retryCountdownContainer == null || retryCountdownText == null)
            {
                Debug.LogWarning("Retry countdown UI is not set up properly.");
                return;
            }
            
            retryCountdownContainer.SetActive(true);

            retryCountdownContainer.transform.DOScale(Vector3.one, retryObjectTweenDuration)
                .SetEase(Ease.OutBack)
                .SetUpdate(true);
            
            // Start with the maximum value
            float countdownValue = retryCountdown;
            // Create a DOTween animation to count down
            var tween = DOTween.To(
                    () => countdownValue,
                    x =>
                    {
                        countdownValue = x;

                        if (retryCountdownText != null)
                        {
                            retryCountdownText.text = Mathf.Ceil(countdownValue).ToString(CultureInfo.InvariantCulture);
                        }
                    },
                    0,
                    retryCountdown)
                .SetEase(Ease.Linear)
                .SetUpdate(true)
                .OnStart(() => retryCountdownContainer.SetActive(true))
                .OnComplete(() =>
                {
                    retryCountdownContainer.transform.DOScale(Vector3.zero, retryObjectTweenDuration)
                        .SetEase(Ease.InBack)
                        .SetUpdate(true)
                        .OnComplete(() =>
                        {
                            retryCountdownContainer.SetActive(false);
                        });
                });
            await tween.WithCancellation(cancellationToken);
        }

        private async UniTaskVoid StartRetryFlowAsync(CancellationToken cancellationToken)
        {
            await TweenRetryCountdown(cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();

            var previousCorrectAnswerPosition = _answerButtons.FindIndex(btn => btn.Answer.IsCorrect);

            Shuffle.FisherYates(_answerButtons);

            // if correct answer position is the same as before shuffling, replace it with a random one
            var correctAnswerPosition = _answerButtons.FindIndex(btn => btn.Answer.IsCorrect);
            if (correctAnswerPosition == previousCorrectAnswerPosition)
            {
                correctAnswerPosition = UnityEngine.Random.Range(0, _answerButtons.Count);
                // swap the correct answer with the new random one using new C# swap syntax
                (_answerButtons[correctAnswerPosition], _answerButtons[previousCorrectAnswerPosition]) = (
                    _answerButtons[previousCorrectAnswerPosition], _answerButtons[correctAnswerPosition]);
            }

            foreach (var btn in _answerButtons)
            {
                if (btn == null)
                {
                    continue;
                }

                btn.ResetState();
                btn.transform.SetAsLastSibling();
            }

            _keyboardNavigationEnabled = true;
            _selectedButtonIndex = -1; // Reset selection
        }

        private int GetRightButtonIndex(int currentIndex)
        {
            // In a 2x2 grid: 0,1 top row; 2,3 bottom row
            // From 0 -> 1, from 2 -> 3, from 1 -> 0 (wrap), from 3 -> 2 (wrap)
            if (currentIndex == 0 && _answerButtons.Count > 1) return 1;
            if (currentIndex == 1) return 0; // Wrap to left
            if (currentIndex == 2 && _answerButtons.Count > 3) return 3;
            if (currentIndex == 3) return 2; // Wrap to left
            return currentIndex;
        }

        private int GetLeftButtonIndex(int currentIndex)
        {
            // Mirror of right navigation
            if (currentIndex == 1) return 0;
            if (currentIndex == 0 && _answerButtons.Count > 1) return 1; // Wrap to right
            if (currentIndex == 3) return 2;
            if (currentIndex == 2 && _answerButtons.Count > 3) return 3; // Wrap to right
            return currentIndex;
        }

        private int GetDownButtonIndex(int currentIndex)
        {
            // From top row (0,1) to bottom row (2,3)
            if (currentIndex == 0 && _answerButtons.Count > 2) return 2;
            if (currentIndex == 1 && _answerButtons.Count > 3) return 3;
            if (currentIndex == 2) return 0; // Wrap to top
            if (currentIndex == 3) return 1; // Wrap to top
            return currentIndex;
        }

        private int GetUpButtonIndex(int currentIndex)
        {
            // From bottom row (2,3) to top row (0,1)
            if (currentIndex == 2) return 0;
            if (currentIndex == 3) return 1;
            if (currentIndex == 0 && _answerButtons.Count > 2) return 2; // Wrap to bottom
            if (currentIndex == 1 && _answerButtons.Count > 3) return 3; // Wrap to bottom
            return currentIndex;
        }

        private void SetSelectedButton(int index)
        {
            if (index < 0 || index >= _answerButtons.Count) return;

            // Remove highlight from previous button
            if (_selectedButtonIndex >= 0 && _selectedButtonIndex < _answerButtons.Count)
            {
                _answerButtons[_selectedButtonIndex].SetKeyboardHighlight(false);
            }

            // Set new selected button
            _selectedButtonIndex = index;
            _answerButtons[_selectedButtonIndex].SetKeyboardHighlight(true);
        }

        private void GenerateAnswerOptions(IQuestion question)
        {
            // Clear existing buttons
            ClearAnswerButtons();

            // Create buttons for each option
            foreach (var answer in question.Choices)
            {
                AnswerButton button = _btnsPool.Get();
                button.Setup(answer, question);
                button.transform.SetAsLastSibling();
                _answerButtons.Add(button);
            }

            // Enable keyboard navigation for FullScreen presentation, but not on mobile
            if (questionPresentationType == QuestionPresentationType.FullScreen)
            {
                _keyboardNavigationEnabled = true;
                _selectedButtonIndex = -1; // No button selected initially
            }

            // Subscribe to timer events if question has a timer
            if (question.Timer != null)
            {
                question.Timer.OnTimerProgressChanged += OnTimerProgressChanged;

                // Show timer UI
                if (clockGameObject != null)
                {
                    clockGameObject.SetActive(true);
                }

                if (timerFillImage != null)
                {
                    timerFillImage.fillAmount = question.Timer.Progress;
                    timerFillImage.color = fullTimeColor;
                }
            }
        }

        private void ClearAnswerButtons()
        {
            // Disable keyboard navigation
            _keyboardNavigationEnabled = false;
            _selectedButtonIndex = -1;

            // Unsubscribe from timer events if current question has a timer
            if (_currentQuestion.Question?.Timer != null)
            {
                _currentQuestion.Question.Timer.OnTimerProgressChanged -= OnTimerProgressChanged;
            }

            // Stop timer UI
            StopTimer();

            foreach (var button in _answerButtons)
            {
                button.SetKeyboardHighlight(false);
                _btnsPool.Release(button);
            }

            _answerButtons.Clear();
        }

        protected override bool ProcessNextQuestion()
        {
            var baseProcessed = base.ProcessNextQuestion();
            if (retryCountdownContainer != null)
            {
                retryCountdownContainer.SetActive(false);
            }

            if (baseProcessed)
            {
                GenerateAnswerOptions(this._currentQuestion.Question);
            }

            return baseProcessed;
        }

        protected override void HideUI(bool animate)
        {
            base.HideUI(animate);
            ClearAnswerButtons();
        }

        private void StopTimer()
        {
            // Timer is now managed by the question provider
            // UI just hides the visual representation
            if (clockGameObject != null)
            {
                clockGameObject.SetActive(false);
            }
        }

        // Timer event handlers
        private void OnTimerProgressChanged(float progress)
        {
            if (timerFillImage != null)
            {
                timerFillImage.fillAmount = progress;

                // Update color based on current fill amount
                Color targetColor = fullTimeColor;

                if (progress <= lowTimeThreshold)
                {
                    targetColor = lowTimeColor;
                }
                else if (progress <= mediumTimeThreshold)
                {
                    targetColor = mediumTimeColor;
                }

                timerFillImage.color = targetColor;
            }
        }
    }
}