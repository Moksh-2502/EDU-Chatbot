using System;
using System.Threading;
using UnityEngine;
using Cysharp.Threading.Tasks;
using ReusablePatterns.FluencySDK.Scripts.Interfaces;
using FluencySDK.Data;

namespace FluencySDK.Unity
{
    public class QuestionAudioPlayer : MonoBehaviour, IQuestionGenerationGate
    {
        [Header("Audio Source")] [SerializeField]
        private AudioSource audioSource;

        [Header("Fluency Configuration")] [SerializeField]
        private Data.FluencyAudioConfig fluencyAudioConfig;

        [Header("Streak Configuration")] [SerializeField]
        private StreakConfiguration streakConfiguration;

        private IQuestionProvider _questionProvider;
        private IQuestionGenerationGateRegistry _questionGenerationGateRegistry;
        private int[] _currentQuestionFactors;
        private bool _isAudioSequencePlaying = false;

        public bool CanGenerateNextQuestion => !_isAudioSequencePlaying;
        public string GateIdentifier => "QuestionAudioPlayer";

        private void Awake()
        {
            // Create AudioSource component if not assigned
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
                audioSource.playOnAwake = false;
                audioSource.ignoreListenerPause = true;
            }

            _questionProvider = BaseQuestionProvider.Instance;
            _questionGenerationGateRegistry = BaseQuestionProvider.Instance;

            if (_questionProvider != null)
            {
                _questionProvider.OnQuestionAnswerSubmitAttempted += OnQuestionSubmitAttempted;
            }
        }

        private void OnEnable()
        {
            IQuestionGameplayHandler.QuestionHandlerStartedEvent += OnQuestionStarted;
            IQuestionGameplayHandler.QuestionHandlerEndedEvent += OnQuestionEnded;

            if (_questionGenerationGateRegistry != null)
            {
                _questionGenerationGateRegistry.RegisterQuestionGenerationGate(this);
            }
        }

        private void OnDisable()
        {
            IQuestionGameplayHandler.QuestionHandlerStartedEvent -= OnQuestionStarted;
            IQuestionGameplayHandler.QuestionHandlerEndedEvent -= OnQuestionEnded;

            if (_questionGenerationGateRegistry != null)
            {
                _questionGenerationGateRegistry.UnregisterQuestionGenerationGate(this);
            }
        }

        private void OnDestroy()
        {
            if (_questionProvider != null)
            {
                _questionProvider.OnQuestionAnswerSubmitAttempted -= OnQuestionSubmitAttempted;
            }
        }
        
        private void OnQuestionSubmitAttempted(IQuestion question, SubmitAnswerResult submissionResult)
        {
            if (fluencyAudioConfig == null || submissionResult.UserAnswerSubmission == null || submissionResult.ShouldRetry == false)
            {
                return;
            }

            var clip = fluencyAudioConfig.GetRandomWrongNoCorrectionClip();
            PlaySound(clip);
        }

        private void OnQuestionStarted(IQuestionGameplayHandler handler, IQuestion question)
        {
            if (fluencyAudioConfig == null)
            {
                return;
            }

            // Extract and store factors from question text for later use
            _currentQuestionFactors = StringUtilities.ExtractFactorsFromQuestionText(question?.Text);

            // Get the appropriate audio clip (factors-specific or fallback to general)
            AudioClip audioClip = handler.QuestionPresentationType == QuestionPresentationType.FullScreen
                ? fluencyAudioConfig.QuestionStartedClip
                : fluencyAudioConfig.GetQuestionStartedAudioClip(_currentQuestionFactors);
            PlaySound(audioClip);
        }

        private void OnQuestionEnded(IQuestionGameplayHandler handler, IQuestion question,
            UserAnswerSubmission userAnswerSubmission)
        {
            if (fluencyAudioConfig == null)
            {
                return;
            }

            var cancellationToken = this.GetCancellationTokenOnDestroy();
            if (userAnswerSubmission.AnswerType == AnswerType.Correct)
            {
                PlayCorrectAnswerAudioAsync(cancellationToken).Forget();
            }
            else
            {
                // Start the wrong answer sequence (prefix -> delay -> correction)
                PlayWrongAnswerSequenceAsync(cancellationToken).Forget();
            }
        }

        /// <summary>
        /// Tracks correct answer audio duration to prevent question generation while playing
        /// </summary>
        private async UniTaskVoid PlayCorrectAnswerAudioAsync(CancellationToken cancellationToken)
        {
            try
            {
                _isAudioSequencePlaying = true;
                // Determine if this is a streak bonus or regular correct answer
                AudioClip correctClip = GetCorrectAnswerAudioClip();
                if (correctClip != null)
                {
                    PlaySound(correctClip);
                    await UniTask.WaitForSeconds(correctClip.length, ignoreTimeScale: true, cancellationToken: cancellationToken);
                }
            }
            catch (OperationCanceledException)
            {
                // Sequence was cancelled, this is expected behavior
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error tracking correct answer audio: {ex.Message}");
            }
            finally
            {
                _isAudioSequencePlaying = false;
            }
        }

        /// <summary>
        /// Async method to play the wrong answer sequence: prefix -> delay -> correction
        /// </summary>
        private async UniTaskVoid PlayWrongAnswerSequenceAsync(CancellationToken cancellationToken)
        {
            try
            {
                _isAudioSequencePlaying = true;

                // Step 1: Play random wrong answer prefix
                AudioClip prefixClip = fluencyAudioConfig.GetRandomWrongAnswerPrefix();
                if (prefixClip != null)
                {
                    PlaySound(prefixClip);
                    await UniTask.WaitForSeconds(prefixClip.length, ignoreTimeScale: true,
                        cancellationToken: cancellationToken);
                }
                else
                {
                    // Fallback to the original wrong answer clip if no prefix available
                    AudioClip wrongClip = fluencyAudioConfig.GetAnswerWrongAudioClip();
                    if (wrongClip != null)
                    {
                        PlaySound(wrongClip);
                        await UniTask.WaitForSeconds(wrongClip.length, ignoreTimeScale: true,
                            cancellationToken: cancellationToken);
                    }
                }

                // Step 2: Wait for the configured delay time
                await UniTask.WaitForSeconds(fluencyAudioConfig.CorrectionDelayTime, ignoreTimeScale: true,
                    cancellationToken: cancellationToken);

                // Step 3: Play the correction audio
                AudioClip correctionClip = fluencyAudioConfig.GetCorrectionAudioClip(_currentQuestionFactors);

                // If no specific correction found, try fallback correction
                if (correctionClip == null)
                {
                    correctionClip = fluencyAudioConfig.GetFallbackCorrectionAudioClip();
                }

                if (correctionClip != null)
                {
                    PlaySound(correctionClip);

                    // Wait for the correction audio to finish playing
                    await UniTask.WaitForSeconds(correctionClip.length, ignoreTimeScale: true,
                        cancellationToken: cancellationToken);
                }
            }
            catch (OperationCanceledException)
            {
                // Sequence was cancelled, this is expected behavior
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error in wrong answer sequence: {ex.Message}");
            }
            finally
            {
                _isAudioSequencePlaying = false;
            }
        }

        /// <summary>
        /// Gets the appropriate audio clip for a correct answer.
        /// Returns streak audio if this is a complete correct streak, otherwise returns regular success audio.
        /// </summary>
        /// <returns>The correct answer audio clip to play</returns>
        private AudioClip GetCorrectAnswerAudioClip()
        {
            if (fluencyAudioConfig == null)
            {
                return null;
            }

            // Check if this is a complete correct streak
            if (IsCompleteCorrectStreak())
            {
                // Try to get a random streak audio clip
                AudioClip streakClip = fluencyAudioConfig.GetRandomStreakAudioClip();
                if (streakClip != null)
                {
                    Debug.Log("[QuestionAudioPlayer] Playing streak bonus audio");
                    return streakClip;
                }
            }

            // Default to random success audio clip
            AudioClip successClip = fluencyAudioConfig.GetRandomSuccessAudioClip();
            if (successClip != null)
            {
                Debug.Log("[QuestionAudioPlayer] Playing regular success audio");
                return successClip;
            }

            // Fallback to the original correct answer clip if no success clips available
            Debug.Log("[QuestionAudioPlayer] Falling back to original correct answer clip");
            return fluencyAudioConfig.GetAnswerCorrectAudioClip();
        }

        /// <summary>
        /// Checks if the current correct streak is a complete streak (divisible by threshold).
        /// </summary>
        /// <returns>True if this is a complete correct streak, false otherwise</returns>
        private bool IsCompleteCorrectStreak()
        {
            if (_questionProvider?.StudentState == null || streakConfiguration == null)
            {
                return false;
            }

            return _questionProvider.StudentState.IsCorrectStreakThresholdReached(streakConfiguration
                .CorrectStreakThreshold);
        }

        private void PlaySound(AudioClip clip)
        {
            if (clip != null && audioSource != null)
            {
                audioSource.clip = clip;
                audioSource.Play();
            }
        }
    }
}