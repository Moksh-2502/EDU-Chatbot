using System;
namespace SubwaySurfers.Tutorial.Events
{
    public class TutorialEventBus
    {
        // Event declarations
        public static event Action<TutorialStepStartedEvent> OnStepStarted;
        public static event Action<TutorialStepCompletedEvent> OnStepCompleted;
        public static event Action<TutorialActionPerformedEvent> OnActionPerformed;
        public static event Action<TutorialProgressEvent> OnProgressChanged;
        public static event Action<TutorialStateChangedEvent> OnStateChanged;
        public static event Action<TutorialStartEvent> OnTutorialStart; 
        public static event Action<TutorialCompletedEvent> OnTutorialCompleted;
        public static event Action<TutorialUIEvent> OnUIEvent;
        public static event Action<TutorialObstacleSpawnRequest> OnObstacleSpawnRequested;
        public static event Action<TutorialObstacleCleanupRequest> OnObstacleCleanupRequested;

        // Event publishing methods
        public static void PublishStepStarted(TutorialStepStartedEvent eventData)
        {
            OnStepStarted?.Invoke(eventData);
        }

        public static void PublishStepCompleted(TutorialStepCompletedEvent eventData)
        {
            OnStepCompleted?.Invoke(eventData);
        }

        public static void PublishActionPerformed(TutorialActionPerformedEvent eventData)
        {
            OnActionPerformed?.Invoke(eventData);
        }

        public static void PublishProgressChanged(TutorialProgressEvent eventData)
        {
            OnProgressChanged?.Invoke(eventData);
        }

        public static void PublishStateChanged(TutorialStateChangedEvent eventData)
        {
            OnStateChanged?.Invoke(eventData);
        }
        
        public static void PublishTutorialStart(TutorialStartEvent eventData)
        {
            OnTutorialStart?.Invoke(eventData);
        }

        public static void PublishTutorialCompleted(TutorialCompletedEvent eventData)
        {
            OnTutorialCompleted?.Invoke(eventData);
        }

        public static void PublishUIEvent(TutorialUIEvent eventData)
        {
            OnUIEvent?.Invoke(eventData);
        }

        public static void PublishObstacleSpawnRequest(TutorialObstacleSpawnRequest eventData)
        {
            OnObstacleSpawnRequested?.Invoke(eventData);
        }

        public static void PublishObstacleCleanupRequest(TutorialObstacleCleanupRequest eventData)
        {
            OnObstacleCleanupRequested?.Invoke(eventData);
        }
    }
} 