using ReusablePatterns.FluencySDK.Scripts.Interfaces;
using ReusablePatterns.SharedCore.Scripts.Runtime;
using SubwaySurfers.Tutorial.Data;
using UnityEngine;
using SubwaySurfers.Tutorial.Events;

namespace SubwaySurfers.Tutorial.Core
{
    public class TutorialAudioPlayer : MonoBehaviour, IQuestionGenerationGate
    {
        public bool CanGenerateNextQuestion => IsPlayingAudio() == false;
        public string GateIdentifier => "TutorialAudioPlayer";
        
        [Header("Audio Source")] [SerializeField]
        private AudioSource audioSource;

        [Header("Tutorial Configuration")] [SerializeField]
        private TutorialConfig tutorialConfig;

        private void Awake()
        {
            // Create AudioSource component if not assigned
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
                audioSource.playOnAwake = false;
                audioSource.ignoreListenerPause = true;
            }
        }

        private void OnEnable()
        {
            TutorialEventBus.OnStepStarted += HandleStepStarted;
        }

        private void OnDisable()
        {
            TutorialEventBus.OnStepStarted -= HandleStepStarted;
        }

        private void HandleStepStarted(TutorialStepStartedEvent stepStartedEvent)
        {
            PlayStepInstructionAudio(stepStartedEvent.StepType);
        }

        private void PlaySound(AudioClip clip)
        {
            if (clip != null && audioSource != null)
            {
                bool isMobile = PlatformDetector.IsMobileBrowser;
                audioSource.clip = clip;
                audioSource.Play();
                Debug.Log(
                    $"[TutorialAudioPlayer] Playing audio clip: {clip.name} (Platform: {(isMobile ? "Mobile" : "Desktop")})");
            }
        }

        /// <summary>
        /// Public method to manually play a specific step's instruction audio (platform-specific)
        /// </summary>
        public void PlayStepInstructionAudio(TutorialStepType stepType)
        {
            if (tutorialConfig == null)
            {
                return;
            }

            var stepData = tutorialConfig.GetStepData(stepType);
            var platformAudio = stepData?.GetPlatformInstructionAudio();
            if (platformAudio != null)
            {
                PlaySound(platformAudio);
            }
        }

        /// <summary>
        /// Public method to check if audio is currently playing
        /// </summary>
        public bool IsPlayingAudio()
        {
            return audioSource != null && audioSource.isPlaying;
        }

        /// <summary>
        /// Public method to stop currently playing audio
        /// </summary>
        public void StopAudio()
        {
            if (audioSource != null && audioSource.isPlaying)
            {
                audioSource.Stop();
            }
        }
    }
}