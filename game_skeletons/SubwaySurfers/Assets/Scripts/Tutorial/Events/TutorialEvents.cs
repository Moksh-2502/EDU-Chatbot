using System;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace SubwaySurfers.Tutorial.Events
{
    public enum TutorialAction
    {
        SwipeLeft,
        SwipeRight,
        SwipeUp,
        SwipeDown,
        ObstacleHit,
        ObstacleAvoided
    }

    public enum TutorialStepType
    {
        LeftRightSwipe = 0,
        SwipeLeft = 1,
        SwipeRight = 2,
        Jump = 3,
        Slide = 4,
        Completion = 5,
        QuestionTutorial = 6,
        Start = 7,
    }

    public enum TutorialObstacleType
    {
        TrashCan,        // For lane blocking (swipe steps)
        LowBarrier,      // For sliding under 
        HighBarrier,     // For jumping over
        TrafficCone      // Alternative for jumping
    }

    public struct TutorialStepStartedEvent
    {
        public TutorialStepType StepType;
        public string StepName;
    }

    public struct TutorialStepCompletedEvent
    {
        public TutorialStepType StepType;
        public string StepName;
        public bool Success;
    }

    public struct TutorialActionPerformedEvent
    {
        public TutorialAction Action;
        public bool WasValid;
        public Vector3 Position;
        public float Timestamp;
    }

    public struct TutorialProgressEvent
    {
        public TutorialStepType CurrentStep;
        public int SuccessfulActions;
        public int RequiredActions;
        public float CompletionPercentage;
    }
    
    public struct TutorialStartEvent
    {
        
    }

    public struct TutorialStateChangedEvent
    {
        public bool IsActive;
        public bool IsPaused;
        public TutorialStepType? CurrentStep;
    }

    public struct TutorialCompletedEvent
    {
        public bool Success;
        public float CompletionTime;
        public int TotalActions;
        public string CompletionReason;
    }

    public struct TutorialUIEvent
    {
        public enum UIEventType
        {
            ShowInstructions,
            HideInstructions,
            UpdateProgress
        }

        public UIEventType EventType;
        public string Message;
    }

    public struct TutorialObstacleSpawnRequest
    {
        public TutorialStepType StepType;
        public TutorialObstacleType ObstacleType;
        public int[] BlockedLanes;          // Which lanes to block (0=left, 1=center, 2=right)
        public float SpawnDistance;         // Distance ahead of player to spawn
        public int GroupCount;              // Number of obstacle groups to spawn
        public float GroupSeparation;       // Distance between groups
    }

    public struct TutorialObstacleCleanupRequest
    {
        public TutorialStepType StepType;
        public bool CleanupAll;
    }
} 