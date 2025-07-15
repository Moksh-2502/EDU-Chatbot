using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System;
using FluencySDK.Unity;

namespace FluencySDK.UI
{
    public class AnswerButton : MonoBehaviour
    {
        [SerializeField] private Image bgImg;
        [SerializeField] private Sprite idleImg, rightImg, wrongImg, highlightedImg;
        [SerializeField] private TMP_Text answerTxt;
        [SerializeField] private Button button;

        [Header("Text Colors")] [SerializeField]
        private Color normalTextColor = Color.black;

        [SerializeField] private Color highlightTextColor = Color.black;
        private ButtonState _currentState = ButtonState.Idle;

        public enum ButtonState
        {
            Idle,
            Correct,
            Wrong,
            Highlighted
        }

        public QuestionChoice<int> Answer { get; private set; }
        public IQuestion Question { get; private set; }

        private IQuestionProvider _questionProvider;

        private void Awake()
        {
            if (button == null)
                button = GetComponent<Button>();

            _questionProvider = BaseQuestionProvider.Instance;
            if (_questionProvider != null)
            {
                _questionProvider.OnQuestionAnswerSubmitAttempted += HandleAnswerSubmitted;
            }

            button.onClick.AddListener(OnButtonClicked);

            // Set initial state
            SetState(ButtonState.Idle);
        }

        private void OnDestroy()
        {
            if (_questionProvider != null)
            {
                _questionProvider.OnQuestionAnswerSubmitAttempted -= HandleAnswerSubmitted;
            }
        }

        private void HandleAnswerSubmitted(IQuestion question, SubmitAnswerResult result)
        {
            SetInteractable(false);
            SetKeyboardHighlight(false);

            if (this.Answer?.Value == result.UserAnswerSubmission?.Answer?.Value || this.Answer is { IsCorrect: true })
            {
                SetState(this.Answer.IsCorrect ? ButtonState.Correct : ButtonState.Wrong);
            }
        }

        public void Setup(QuestionChoice<int> answer, IQuestion question)
        {
            Answer = answer;
            Question = question;
            answerTxt.text = gameObject.name = answer.Value.ToString();
            SetInteractable(true);
            SetState(ButtonState.Idle);
            SetKeyboardHighlight(false); // Reset keyboard highlight
        }

        private void SetState(ButtonState state)
        {
            _currentState = state;
            UpdateVisualState();
        }

        public void SetInteractable(bool interactable)
        {
            button.interactable = interactable;
        }

        /// <summary>
        /// Sets the keyboard highlight state for this button
        /// </summary>
        /// <param name="highlighted">Whether the button should be highlighted</param>
        public void SetKeyboardHighlight(bool highlighted)
        {
            if (highlighted)
            {
                SetState(ButtonState.Highlighted);
            }
            else
            {
                // Only reset to idle if currently highlighted
                if (_currentState == ButtonState.Highlighted)
                {
                    SetState(ButtonState.Idle);
                }
            }
        }

        /// <summary>
        /// Simulates a button click programmatically (for keyboard navigation)
        /// </summary>
        public void SimulateClick()
        {
            if (button.interactable)
            {
                OnButtonClicked();
            }
        }

        /// <summary>
        /// Resets button to idle state
        /// </summary>
        public void ResetState()
        {
            SetState(ButtonState.Idle);
            SetInteractable(true);
        }

        private void UpdateVisualState()
        {
            // Apply appropriate sprite and text color based on current state
            switch (_currentState)
            {
                case ButtonState.Idle:
                    bgImg.sprite = idleImg;
                    answerTxt.color = normalTextColor;
                    break;
                case ButtonState.Correct:
                    bgImg.sprite = rightImg;
                    answerTxt.color = normalTextColor;
                    break;
                case ButtonState.Wrong:
                    bgImg.sprite = wrongImg;
                    answerTxt.color = normalTextColor;
                    break;
                case ButtonState.Highlighted:
                    bgImg.sprite = highlightedImg != null ? highlightedImg : idleImg;
                    answerTxt.color = highlightTextColor;
                    break;
            }
        }


        private void OnButtonClicked()
        {
            _questionProvider.SubmitAnswer(this.Question,
                UserAnswerSubmission.FromAnswer(this.Answer));
        }
    }
}