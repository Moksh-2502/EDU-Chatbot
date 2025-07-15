using UnityEngine;
using AIEduChatbot.UnityReactBridge.Core;
using AIEduChatbot.UnityReactBridge.Data;
namespace AIEduChatbot.SharedCore
{
    /// <summary>
    /// Service for handling feedback functionality across the game
    /// Provides methods to give feedback either through React bridge or direct URL opening
    /// </summary>
    public static class FeedbackService
    {
        private const string FEEDBACK_URL = "https://docs.google.com/forms/d/e/1FAIpQLScLNB8uP_wrdDLBJ2J2E28BtxbK1oYyfBOWPu-ubOcuqgVvPQ/viewform?usp=header";

        /// <summary>
        /// Opens the feedback form. Uses React bridge if available, otherwise opens URL directly.
        /// </summary>
        /// <param name="data">Additional feedback data to send (optional)</param>
        public static void GiveFeedback(FeedbackData data = null)
        {
            Debug.Log("[FeedbackService] GiveFeedback called");

            // Check if React bridge is available
            if (ReactBridge.IsAvailable)
            {
                Debug.Log("[FeedbackService] React bridge available, sending GiveFeedbackMessage");

                // Send message through React bridge
                var feedbackMessage = new GiveFeedbackMessage(data);
                ReactBridge.SendGameMessage(feedbackMessage);
            }
            else
            {
                Debug.Log("[FeedbackService] React bridge not available, opening URL directly");

                // Fallback to direct URL opening
                Application.OpenURL(FEEDBACK_URL);
            }
        }

        /// <summary>
        /// Opens the feedback form with default Unity source data
        /// </summary>
        public static void GiveFeedback()
        {
            GiveFeedback(new FeedbackData
            {
                Source = "unity",
                Metadata = new { origin = "game_button" }
            });
        }
    }
}