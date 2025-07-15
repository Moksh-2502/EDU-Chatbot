using TMPro;
using UnityEngine;
using ReusablePatterns.FluencySDK.Scripts.Interfaces;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine.UI;
using System.Linq;
using FluencySDK.Data;
namespace FluencySDK.UI
{
    public abstract class BaseQuestionUI : MonoBehaviour
    {
        [SerializeField] private QuestionPresentationConfiguration questionPresentationConfiguration;
        [SerializeField] private RectTransform root;
        [SerializeField] private float animationDuration = 0.3f;
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private TMP_Text[] questionTxt;
        [SerializeField] protected QuestionPresentationType questionPresentationType;
        
        protected QuestionData _currentQuestion;
        private Sequence _uiAnimationSequence;
        
        private bool _isEnabled = false;

        // Struct to store question data
        protected struct QuestionData
        {
            public QuestionPresentationType PresentationType;
            public float PostAnswerPresentationTime;
            public IQuestion Question;
        }

        private void Awake()
        {
            Initialize();
        }

        protected virtual void OnEnable()
        {
            _isEnabled = true;
        }

        protected virtual void OnDisable()
        {
            _isEnabled = false;
        }

        protected virtual void Initialize()
        {
            // Hide the UI initially
            HideUI(false);
            IQuestionGameplayHandler.QuestionHandlerStartedEvent += OnQuestionStarted;
            IQuestionGameplayHandler.QuestionHandlerEndedEvent += OnQuestionEnded;
        }

        protected virtual void OnDestroy()
        {
            // Kill any active tweens
            if (_uiAnimationSequence != null && _uiAnimationSequence.IsActive())
            {
                _uiAnimationSequence.Kill();
                _uiAnimationSequence = null;
            }
        }

        private void OnQuestionStarted(IQuestionGameplayHandler handler, IQuestion question)
        {
            if (handler.QuestionPresentationType != questionPresentationType)
            {
                HideUI(false);
                return;
            }
            _currentQuestion = new QuestionData()
            {
                PresentationType = handler.QuestionPresentationType,
                PostAnswerPresentationTime = questionPresentationConfiguration.PostAnswerPresentationTime,
                Question = question
            };
            ProcessNextQuestion();
        }

        protected virtual bool ProcessNextQuestion()
        {
            // Display question
            foreach (var txt in questionTxt)
            {
                if (txt)
                {
                    txt.text = _currentQuestion.Question.Text;
                    LayoutRebuilder.ForceRebuildLayoutImmediate(txt.rectTransform);
                }
            }

            LayoutRebuilder.ForceRebuildLayoutImmediate(root);
            // Show the UI
            SetUIVisible(true, true);
            return true;
        }

        protected virtual void OnQuestionEnded(IQuestionGameplayHandler handler, IQuestion question, UserAnswerSubmission userAnswerSubmission)
        {
            // Only process the next question if the ended question is the current one
            if (_currentQuestion.Question != null && question.Id == _currentQuestion.Question.Id)
            {
                #pragma warning disable CS4014
                ShowFeedbackThenProceed().Forget();
                #pragma warning restore CS4014
            }
        }

        private async UniTask ShowFeedbackThenProceed()
        {
            var ct = this.GetCancellationTokenOnDestroy();
            await UniTask.WaitForSeconds(_currentQuestion.PostAnswerPresentationTime, ignoreTimeScale: true, cancellationToken:
                ct);
            if (ct.IsCancellationRequested)
            {
                return;
            }
            HideUI(_isEnabled);
        }

        protected virtual void HideUI(bool animate)
        {
            SetUIVisible(false, animate);
        }

        private void SetUIVisible(bool visible, bool animate)
        {
            // Kill any ongoing animation
            if (_uiAnimationSequence != null && _uiAnimationSequence.IsActive())
            {
                _uiAnimationSequence.Kill();
                _uiAnimationSequence = null;
            }

            // Set interactivity immediately
            canvasGroup.interactable = visible;
            canvasGroup.blocksRaycasts = visible;

            if (animate && gameObject.activeInHierarchy)
            {
                _uiAnimationSequence = DOTween.Sequence();
                // Make animations ignore pause status
                _uiAnimationSequence.SetUpdate(UpdateType.Normal, true);
                
                // Scale animation with elastic effect if showing UI
                _uiAnimationSequence.Append(root.DOScale(
                    visible ? Vector3.one : Vector3.zero, 
                    animationDuration
                ).SetEase(visible ? Ease.OutBack : Ease.InBack));
                
                // Fade animation
                _uiAnimationSequence.Join(canvasGroup.DOFade(
                    visible ? 1f : 0f, 
                    animationDuration
                ).SetEase(Ease.InOutQuad));
            }
            else
            {
                // Set immediately without animation
                root.localScale = visible ? Vector3.one : Vector3.zero;
                canvasGroup.alpha = visible ? 1f : 0f;
            }
        }
    }
}