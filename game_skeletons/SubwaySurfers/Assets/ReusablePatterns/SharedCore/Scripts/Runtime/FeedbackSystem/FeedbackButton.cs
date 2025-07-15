using UnityEngine;
using UnityEngine.UI;
namespace AIEduChatbot.SharedCore
{
    public class FeedbackButton : MonoBehaviour
    {
        [SerializeField] private Button _feedbackButton;

        private void Awake()
        {
            _feedbackButton.onClick.AddListener(GiveFeedback);
        }

        private void GiveFeedback()
        {
            FeedbackService.GiveFeedback();
        }
    }
}