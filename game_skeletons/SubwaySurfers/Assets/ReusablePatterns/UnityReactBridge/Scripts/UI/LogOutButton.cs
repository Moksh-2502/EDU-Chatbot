using AIEduChatbot.UnityReactBridge.Core;
using AIEduChatbot.UnityReactBridge.Data;
using UnityEngine;
using UnityEngine.UI;

namespace ReusablePatterns.UnityReactBridge.Scripts.UI
{
    /// <summary>
    /// Example script showing how to implement logout functionality from Unity
    /// Attach this script to a GameObject with a Button component to create a logout button
    /// </summary>
    public class LogOutButton : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private Button logoutButton;

        [Header("Settings")]
        [SerializeField] private string logoutReason = "user_requested";

        private void Start()
        {
            // If no button is assigned, try to find one on this GameObject
            if (logoutButton == null)
            {
                logoutButton = GetComponent<Button>();
            }

            // Setup the logout button click event
            if (logoutButton != null)
            {
                logoutButton.onClick.AddListener(OnLogoutButtonClicked);
                Debug.Log("[LogOutButton] Logout button configured");
            }
            else
            {
                Debug.LogWarning("[LogOutButton] No logout button found! Assign a Button component.");
            }
        }

        private void OnDestroy()
        {
            // Clean up the button listener
            if (logoutButton != null)
            {
                logoutButton.onClick.RemoveListener(OnLogoutButtonClicked);
            }
        }

        /// <summary>
        /// Called when the logout button is clicked
        /// </summary>
        private void OnLogoutButtonClicked()
        {
            Debug.Log("[LogOutButton] Logout button clicked");

            // Check if React bridge is available before sending the message
            if (ReactBridge.IsAvailable)
            {
                // Send logout message to React
                ReactBridge.SendGameMessage(new LogoutMessage(logoutReason));

                // Optional: Disable the button to prevent multiple clicks
                if (logoutButton != null)
                {
                    logoutButton.interactable = false;
                }
            }
            else
            {
                Debug.LogWarning("[LogOutButton] React bridge not available - logout message not sent");
            }
        }
    }
}