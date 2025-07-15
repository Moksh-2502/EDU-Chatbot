using FluencySDK;
using UnityEngine;
using TMPro;
using DG.Tweening;

namespace EducationIntegration.QuestionHandlers
{
    public class AnswerObject : MonoBehaviour
    {
        [SerializeField] private TMP_Text answerText;
        [SerializeField] private float zDistance;
        
        public QuestionChoice<int> Answer { get; private set; }
        
        public void Repaint(QuestionChoice<int> answer)
        {
            this.Answer = answer;
            if (answerText != null)
            {
                answerText.text = answer.Value.ToString();
            }
        }
    }
} 