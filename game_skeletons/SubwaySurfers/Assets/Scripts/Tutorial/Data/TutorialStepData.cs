using ReusablePatterns.SharedCore.Scripts.Runtime;
using SubwaySurfers.Tutorial.Events;
using UnityEngine;

namespace SubwaySurfers.Tutorial.Data
{
    [System.Serializable]
    public class TutorialStepData
    {
        public TutorialStepType stepType;
        public string stepName;
        
        [Header("Instructions")]
        public string instructionsMobile;
        public string instructionsDesktop;
        
        [Header("Audio")]
        public AudioClip instructionAudioMobile;
        public AudioClip instructionAudioDesktop;
        
        [Header("Settings")]
        public int requiredSuccessfulActions = 1;
        public bool allowSkipping = false;
        public float autoCompleteDelay = 2f;
        public GameObject uiObject; // UI prefab to instantiate for this step
        
        /// <summary>
        /// Gets the appropriate instruction text based on platform
        /// </summary>
        public string GetPlatformInstructions()
        {
            bool isMobile = PlatformDetector.IsMobileBrowser;
            return isMobile ? instructionsMobile : instructionsDesktop;
        }
        
        /// <summary>
        /// Gets the appropriate instruction audio based on platform
        /// </summary>
        public AudioClip GetPlatformInstructionAudio()
        {
            bool isMobile = PlatformDetector.IsMobileBrowser;
            return isMobile ? instructionAudioMobile : instructionAudioDesktop;
        }
    }
}