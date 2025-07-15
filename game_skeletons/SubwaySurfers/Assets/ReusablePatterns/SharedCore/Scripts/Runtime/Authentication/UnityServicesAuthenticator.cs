using System;
using AIEduChatbot.UnityReactBridge.Handlers;
using Cysharp.Threading.Tasks;
using Unity.Services.Authentication;
using Unity.Services.Core;
using UnityEngine;

namespace ReusablePatterns.SharedCore.Scripts.Runtime.Authentication
{
    public class UnityServicesAuthenticator : MonoBehaviour
    {
        private static bool _isSigningIn = false;
        private static string _currentSignInEmail = "";
        private static UniTaskCompletionSource _currentSignInTask;

        // Configuration for OIDC (OpenID Connect) - should match your Cognito setup
        private const string OIDC_PROVIDER_ID = "oidc-cognito"; // This should match your Unity Dashboard configuration

        private void OnEnable()
        {
            IGameSessionProvider.Instance.OnGameSessionChanged += HandleGameSessionChanged;
        }

        private void OnDisable()
        {
            IGameSessionProvider.Instance.OnGameSessionChanged -= HandleGameSessionChanged;
        }

        private async UniTaskVoid ProcessSessionChangeAsync(IGameSessionProvider gameSessionProvider)
        {
            await EnsureUnityServicesInitialized();
            if (gameSessionProvider.UserData != null)
            {
                if (gameSessionProvider.UserData.HasCognitoIdToken())
                {
                    var userEmail = gameSessionProvider.UserData.email ?? "";

                    // Check if we're already signing in for the same user
                    if (_isSigningIn && _currentSignInEmail == userEmail)
                    {
                        Debug.Log($"Sign-in already in progress for user: {userEmail}. Waiting for completion...");
                        WaitForCurrentSignIn().Forget();
                        return;
                    }

                    // Check if we're already signed in as the same user
                    if (AuthenticationService.Instance.IsSignedIn &&
                        AuthenticationService.Instance.PlayerId != null)
                    {
                        Debug.Log($"User already signed in: {userEmail}");
                        return;
                    }

                    SignInWithCognitoTokenAsync(gameSessionProvider.UserData).Forget();
                    Debug.Log(
                        $"Initiating Cognito authentication for: {gameSessionProvider.UserData.name} ({gameSessionProvider.UserData.email})");
                }
                else
                {
                    if (Application.isEditor)
                    {
                        SignInAnonymouslyAsync().Forget();
                    }
                    else
                    {
                        Debug.LogWarning(
                            "User data does not contain a valid Cognito ID token. Please ensure the user is authenticated with Cognito.");
                    }
                }
            }
            else
            {
                Debug.LogWarning("User data is invalid or not set. Please ensure the user is authenticated.");
            }
        }

        private void HandleGameSessionChanged(IGameSessionProvider gameSessionProvider)
        {
            ProcessSessionChangeAsync(gameSessionProvider).Forget();
        }

        private async UniTask WaitForCurrentSignIn()
        {
            if (_currentSignInTask != null)
            {
                try
                {
                    await _currentSignInTask.Task;
                    Debug.Log("Existing sign-in operation completed successfully.");
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Existing sign-in operation failed: {ex.Message}");
                }
            }
        }

        private async UniTask SignInAnonymouslyAsync()
        {
            if (_isSigningIn)
            {
                Debug.Log("Sign-in operation already in progress. Skipping anonymous sign-in.");
                return;
            }

            _isSigningIn = true;
            _currentSignInEmail = "";
            _currentSignInTask = new UniTaskCompletionSource();

            try
            {
                await EnsureUnityServicesInitialized();

                // Check if user is already signed in
                if (AuthenticationService.Instance.IsSignedIn)
                {
                    Debug.Log("User is already signed in to Unity Authentication Service.");
                    _currentSignInTask.TrySetResult();
                    return;
                }

                await AuthenticationService.Instance
                    .SignInAnonymouslyAsync(new SignInOptions()
                    {
                        CreateAccount = true,
                    });
                Debug.Log("Unity anonymous sign-in successful!");
                _currentSignInTask.TrySetResult();
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
                _currentSignInTask.TrySetException(ex);
                throw;
            }
            finally
            {
                _isSigningIn = false;
                _currentSignInEmail = "";
                _currentSignInTask = null;
            }
        }

        private async UniTask SignInWithCognitoTokenAsync(AIEduChatbot.UnityReactBridge.Data.UserData userData)
        {
            var userEmail = userData.email ?? "";

            if (_isSigningIn)
            {
                if (_currentSignInEmail == userEmail)
                {
                    Debug.Log($"Sign-in already in progress for the same user: {userEmail}. Waiting...");
                    await WaitForCurrentSignIn();
                    return;
                }
                else
                {
                    Debug.LogWarning($"Sign-in in progress for different user. Current: {_currentSignInEmail}, Requested: {userEmail}");
                    return;
                }
            }

            // Check token expiration using the clean UserData method
            if (userData.IsTokenExpired())
            {
                Debug.LogError($"Cognito ID token has expired for user {userEmail}. Time to expiry: {userData.GetTimeToExpiry()}ms. Please refresh your authentication on the React side.");
                return;
            }

            // Log token status for debugging
            var timeToExpiry = userData.GetTimeToExpiry();
            Debug.Log($"Token validation for {userEmail}: Valid for {timeToExpiry}ms ({timeToExpiry / 1000}s)");

            if (timeToExpiry < 5 * 60 * 1000) // Less than 5 minutes
            {
                Debug.LogWarning($"Cognito ID token expires soon for user {userEmail}: {timeToExpiry / 1000}s remaining");
            }

            _isSigningIn = true;
            _currentSignInEmail = userEmail;
            _currentSignInTask = new UniTaskCompletionSource();

            try
            {
                await EnsureUnityServicesInitialized();

                // Check if user is already signed in
                if (AuthenticationService.Instance.IsSignedIn)
                {
                    Debug.Log("User is already signed in to Unity Authentication Service.");
                    _currentSignInTask.TrySetResult();
                    return;
                }

                // Use External Token authentication with Custom OIDC provider
                // This is the new approach for Cognito OIDC authentication
                await AuthenticationService.Instance
                    .SignInWithOpenIdConnectAsync(OIDC_PROVIDER_ID, userData.cognitoIdToken, new SignInOptions() { CreateAccount = true });
                Debug.Log($"Unity Cognito OIDC sign-in successful for user: {userEmail}!");
                _currentSignInTask.TrySetResult();
            }
            catch (AuthenticationException ex) when (ex.Message.Contains("token is expired"))
            {
                Debug.LogError($"Cognito ID token expired during sign-in for user {userEmail}. " +
                              "This may happen if there's a delay between token generation and usage. " +
                              "Please refresh your authentication on the React side.");
                _currentSignInTask.TrySetException(ex);
            }
            catch (AuthenticationException ex) when (ex.Message.Contains("already signing in"))
            {
                Debug.LogWarning($"Authentication service reports sign-in already in progress. This should not happen with our state management.");
                _currentSignInTask.TrySetException(ex);
            }
            catch (AuthenticationException ex) when (ex.Message.Contains("Invalid provider"))
            {
                Debug.LogError($"OIDC provider '{OIDC_PROVIDER_ID}' not configured in Unity Dashboard. " +
                              "Please ensure the Custom OIDC provider is properly set up in your Unity project settings.");
                _currentSignInTask.TrySetException(ex);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Unity Cognito OIDC sign-in failed for user {userEmail}: {ex.Message}");
                Debug.LogException(ex);
                _currentSignInTask.TrySetException(ex);
                throw;
            }
            finally
            {
                _isSigningIn = false;
                _currentSignInEmail = "";
                _currentSignInTask = null;
            }
        }

        private async UniTask EnsureUnityServicesInitialized()
        {
            try
            {
                switch (UnityServices.Instance.State)
                {
                    case ServicesInitializationState.Uninitialized:
                        Debug.Log("[UnityServicesAuthenticator] Initializing Unity Services...");
                        await UnityServices.InitializeAsync();
                        Debug.Log("[UnityServicesAuthenticator] Unity Services initialized successfully.");
                        break;
                    case ServicesInitializationState.Initializing:
                        Debug.Log("[UnityServicesAuthenticator] Unity Services initialization in progress, waiting...");
                        await UniTask.WaitUntil(() =>
                            UnityServices.Instance.State == ServicesInitializationState.Initialized);
                        Debug.Log("[UnityServicesAuthenticator] Unity Services initialization completed.");
                        break;
                    case ServicesInitializationState.Initialized:
                        Debug.Log("[UnityServicesAuthenticator] Unity Services already initialized.");
                        break;
                    default:
                        throw new InvalidOperationException($"Unexpected Unity Services state: {UnityServices.Instance.State}");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[UnityServicesAuthenticator] Failed to initialize Unity Services: {ex.Message}");
                throw new InvalidOperationException("Unity Services initialization failed. Please check your project configuration.", ex);
            }
        }
    }
}