using UnityEngine;
using SubwaySurfers.Tutorial.Events;

namespace SubwaySurfers.Tutorial.Data
{
    [CreateAssetMenu(fileName = "TutorialObstacleSequence", menuName = "Trash Dash/Tutorial/Obstacle Sequence")]
    public class TutorialObstacleSequence : ScriptableObject
    {
        [Header("Sequence Configuration")]
        [SerializeField] private TutorialStepType targetStepType = TutorialStepType.SwipeLeft;
        [SerializeField] private string sequenceName = "Swipe Left Sequence";
        
        [Header("Obstacle Groups")]
        [SerializeField] private TutorialObstacleGroup[] obstacleGroups = {
            new TutorialObstacleGroup()
        };

        // Public properties
        public TutorialStepType TargetStepType => targetStepType;
        public string SequenceName => sequenceName;
        public TutorialObstacleGroup[] ObstacleGroups => obstacleGroups;

        /// <summary>
        /// Validates the obstacle sequence configuration
        /// </summary>
        public bool ValidateConfiguration()
        {
            if (obstacleGroups == null || obstacleGroups.Length == 0)
            {
                Debug.LogWarning($"TutorialObstacleSequence '{name}': No obstacle groups configured");
                return false;
            }

            foreach (var group in obstacleGroups)
            {
                if (group.obstaclePrefab == null || !group.obstaclePrefab.RuntimeKeyIsValid())
                {
                    Debug.LogWarning($"TutorialObstacleSequence '{name}': Invalid obstacle prefab in group");
                    return false;
                }

                if (!group.ValidateLanes())
                {
                    Debug.LogWarning($"TutorialObstacleSequence '{name}': Invalid lane configuration in group");
                    return false;
                }

                if (group.groupCount <= 0 || group.spawnDistance <= 0)
                {
                    Debug.LogWarning($"TutorialObstacleSequence '{name}': Invalid spawn configuration in group");
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Gets obstacle spawn requests for this sequence
        /// </summary>
        public TutorialObstacleSpawnRequest[] GetSpawnRequests()
        {
            var requests = new TutorialObstacleSpawnRequest[obstacleGroups.Length];
            
            for (int i = 0; i < obstacleGroups.Length; i++)
            {
                var group = obstacleGroups[i];
                requests[i] = new TutorialObstacleSpawnRequest
                {
                    StepType = targetStepType,
                    ObstacleType = group.obstacleType,
                    BlockedLanes = group.blockedLanes,
                    SpawnDistance = group.spawnDistance,
                    GroupCount = group.groupCount,
                    GroupSeparation = group.groupSeparation
                };
            }
            
            return requests;
        }
    }
} 