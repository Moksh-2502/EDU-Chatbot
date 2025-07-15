using UnityEngine;
using SubwaySurfers.Tutorial.Events;

namespace SubwaySurfers.Tutorial.Data
{
    [CreateAssetMenu(fileName = "TutorialConfig", menuName = "Trash Dash/Tutorial/Tutorial Config")]
    public class TutorialConfig : ScriptableObject
    {
        [Header("General Settings")]
        [field: SerializeField] public string Version { get; private set; } = "1.0.0";
        [SerializeField] private bool allowSkipping = false;
        [SerializeField] private bool allowRestarting = true;
        [SerializeField] private float stepTransitionDelay = 1f;

        [Header("Tutorial Steps")]
        [SerializeField] private TutorialStepData[] steps = {
            new()
            {
                stepType = TutorialStepType.Start,
                stepName = "Tutorial Start",
                instructionsMobile = "Welcome to the Subway Surfers tutorial!",
                instructionsDesktop = "Welcome to the Subway Surfers tutorial!",
                requiredSuccessfulActions = 1,
                uiObject = null // Assign start UI prefab here
            },
            new()
            {
                stepType = TutorialStepType.SwipeLeft,
                stepName = "Swipe Left",
                instructionsMobile = "Swipe left to move to the left lane",
                instructionsDesktop = "Press A or Left Arrow to move to the left lane",
                requiredSuccessfulActions = 1,
                uiObject = null // Assign swipe left UI prefab here
            },
            new()
            {
                stepType = TutorialStepType.SwipeRight,
                stepName = "Swipe Right",
                instructionsMobile = "Swipe right to move to the right lane",
                instructionsDesktop = "Press D or Right Arrow to move to the right lane",
                requiredSuccessfulActions = 1,
                uiObject = null // Assign swipe right UI prefab here
            },
            new()
            {
                stepType = TutorialStepType.Jump,
                stepName = "Jump",
                instructionsMobile = "Swipe up to jump over obstacles",
                instructionsDesktop = "Press W or Space to jump over obstacles",
                requiredSuccessfulActions = 1,
                uiObject = null // Assign jump UI prefab here
            },
            new()
            {
                stepType = TutorialStepType.Slide,
                stepName = "Slide",
                instructionsMobile = "Swipe down to slide under obstacles",
                instructionsDesktop = "Press S to slide under obstacles",
                requiredSuccessfulActions = 1,
                uiObject = null // Assign slide UI prefab here
            },
            new()
            {
                stepType = TutorialStepType.Completion,
                stepName = "Tutorial Complete",
                instructionsMobile = "Great! You've learned all the moves!",
                instructionsDesktop = "Great! You've learned all the moves!",
                requiredSuccessfulActions = 1,
                uiObject = null // Assign completion UI prefab here
            }
        };

        [Header("UI Settings")]
        [SerializeField] private float uiShowDuration = 3f;
        [SerializeField] private bool showStepCounter = true;

        // Public properties
        public bool AllowSkipping => allowSkipping;
        public bool AllowRestarting => allowRestarting;
        public float StepTransitionDelay => stepTransitionDelay;
        public TutorialStepData[] Steps => steps;
        public float UIShowDuration => uiShowDuration;
        public bool ShowStepCounter => showStepCounter;

        public TutorialStepData GetStepData(TutorialStepType stepType)
        {
            foreach (var step in steps)
            {
                if (step.stepType == stepType)
                    return step;
            }
            return null;
        }

        public int GetStepIndex(TutorialStepType stepType)
        {
            for (int i = 0; i < steps.Length; i++)
            {
                if (steps[i].stepType == stepType)
                    return i;
            }
            return -1;
        }

        public TutorialStepType? GetNextStepType(TutorialStepType currentStep)
        {
            int currentIndex = GetStepIndex(currentStep);
            if (currentIndex >= 0 && currentIndex < steps.Length - 1)
            {
                return steps[currentIndex + 1].stepType;
            }
            return null;
        }
    }
} 