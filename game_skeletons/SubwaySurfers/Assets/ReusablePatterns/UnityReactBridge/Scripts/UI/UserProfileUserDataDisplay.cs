using System;
using System.Threading;
using AIEduChatbot.UnityReactBridge.Data;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

namespace AIEduChatbot.UnityReactBridge.Handlers
{
    public class UserProfileUserDataDisplay : MonoBehaviour, IReactGameMessageHandler
    {
        [SerializeField] private RawImage profileImage;
        [SerializeField] private TMP_Text userNameText, emailText;

        private CancellationTokenSource _imageLoadCts;

        private void Start()
        {
            RepaintFromUserData(IGameSessionProvider.Instance.UserData, "Start");
        }

        private void OnEnable()
        {
            IReactGameMessageHandlerCollection.Instance.RegisterHandler(this);
        }
        private void OnDisable()
        {
            IReactGameMessageHandlerCollection.Instance.UnregisterHandler(this);
            _imageLoadCts?.Cancel();
        }

        private async UniTaskVoid DownloadImageAsync(string url, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(url))
            {
                return;
            }
            try
            {
                var imageGetRequest = UnityWebRequestTexture.GetTexture(url);
                await imageGetRequest.SendWebRequest().WithCancellation(cancellationToken);
                cancellationToken.ThrowIfCancellationRequested();
                if (imageGetRequest.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogError($"[ReactBridge] Error downloading image: {imageGetRequest.error}");
                    return;
                }

                var texture = DownloadHandlerTexture.GetContent(imageGetRequest);
                if (profileImage != null)
                {
                    profileImage.texture = texture;
                }
            }
            catch (OperationCanceledException)
            {

            }
            catch (Exception ex)
            {
                Debug.LogError($"[ReactBridge] Error downloading image: {ex.Message}");
            }
        }

        private void RepaintFromUserData(UserData userData, string source)
        {
            if (userData == null)
            {
                Debug.LogWarning($"[UserProfileUserDataDisplay] Received null user data from {source}.");
                return;

            }
            _imageLoadCts?.Cancel();
            _imageLoadCts = new CancellationTokenSource();

            DownloadImageAsync(userData.image, _imageLoadCts.Token).Forget();

            if (userNameText != null)
            {
                userNameText.text = string.IsNullOrEmpty(userData.name) ? "Unknown User" : userData.name;
            }

            if (emailText != null)
            {
                emailText.text = string.IsNullOrEmpty(userData.email) ? "No Email Provided" : userData.email;
            }
        }

        public void OnGameEventReceived(ReactGameMessage gameMessage)
        {
            if (gameMessage is SessionDataReactGameMessage userDataPayload)
            {
                RepaintFromUserData(userDataPayload.user, "OnGameEventReceived");
            }
        }
    }
}
