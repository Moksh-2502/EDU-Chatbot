using System;
using UnityEngine;
using SubwaySurfers.Tutorial.Events;

namespace SubwaySurfers.Tutorial.Data
{
    /// <summary>
    /// JSON serializable version of TutorialObstacleGroup for editor import/export
    /// </summary>
    [Serializable]
    public class TutorialObstacleGroupJson
    {
        public string obstacleType = "TrashCan";
        public string obstaclePrefabAddress = "ObstacleBin";
        public int[] blockedLanes = { 1, 2 };
        public float spawnDistance = 15f;
        public int groupCount = 2;
        public float groupSeparation = 5f;

        /// <summary>
        /// Converts JSON data to TutorialObstacleGroup
        /// </summary>
        public TutorialObstacleGroup ToObstacleGroup()
        {
            var group = new TutorialObstacleGroup();
            
            // Parse obstacle type enum
            if (Enum.TryParse<TutorialObstacleType>(obstacleType, out var parsedType))
            {
                group.obstacleType = parsedType;
            }
            else
            {
                Debug.LogWarning($"Invalid obstacle type: {obstacleType}, defaulting to TrashCan");
                group.obstacleType = TutorialObstacleType.TrashCan;
            }

            // Set addressable reference
            group.obstaclePrefab = new UnityEngine.AddressableAssets.AssetReference(obstaclePrefabAddress);
            
            // Copy configuration
            group.blockedLanes = blockedLanes ?? new int[] { 1, 2 };
            group.spawnDistance = spawnDistance;
            group.groupCount = groupCount;
            group.groupSeparation = groupSeparation;

            return group;
        }

        /// <summary>
        /// Creates JSON data from TutorialObstacleGroup
        /// </summary>
        public static TutorialObstacleGroupJson FromObstacleGroup(TutorialObstacleGroup group)
        {
            return new TutorialObstacleGroupJson
            {
                obstacleType = group.obstacleType.ToString(),
                obstaclePrefabAddress = group.obstaclePrefab?.AssetGUID ?? "",
                blockedLanes = group.blockedLanes,
                spawnDistance = group.spawnDistance,
                groupCount = group.groupCount,
                groupSeparation = group.groupSeparation
            };
        }
    }

    /// <summary>
    /// JSON serializable version of TutorialObstacleSequence for editor import/export
    /// </summary>
    [Serializable]
    public class TutorialObstacleSequenceJson
    {
        public string targetStepType = "SwipeLeft";
        public string sequenceName = "Tutorial Sequence";
        public TutorialObstacleGroupJson[] obstacleGroups = new TutorialObstacleGroupJson[0];

        /// <summary>
        /// Applies JSON data to existing TutorialObstacleSequence ScriptableObject
        /// </summary>
        public void ApplyToSequence(TutorialObstacleSequence sequence)
        {
            if (sequence == null)
            {
                Debug.LogError("Cannot apply JSON data to null sequence");
                return;
            }

            // Parse step type enum
            if (Enum.TryParse<TutorialStepType>(targetStepType, out var parsedStepType))
            {
                // Use reflection to set private fields since they don't have public setters
                var stepTypeField = typeof(TutorialObstacleSequence).GetField("targetStepType", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                stepTypeField?.SetValue(sequence, parsedStepType);
            }
            else
            {
                Debug.LogWarning($"Invalid step type: {targetStepType}");
            }

            // Set sequence name
            var nameField = typeof(TutorialObstacleSequence).GetField("sequenceName", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            nameField?.SetValue(sequence, sequenceName);

            // Convert and set obstacle groups
            var groups = new TutorialObstacleGroup[obstacleGroups.Length];
            for (int i = 0; i < obstacleGroups.Length; i++)
            {
                groups[i] = obstacleGroups[i].ToObstacleGroup();
            }

            var groupsField = typeof(TutorialObstacleSequence).GetField("obstacleGroups", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            groupsField?.SetValue(sequence, groups);

            // Mark as dirty for Unity to save changes
#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(sequence);
#endif
        }

        /// <summary>
        /// Creates JSON data from existing TutorialObstacleSequence
        /// </summary>
        public static TutorialObstacleSequenceJson FromSequence(TutorialObstacleSequence sequence)
        {
            var json = new TutorialObstacleSequenceJson
            {
                targetStepType = sequence.TargetStepType.ToString(),
                sequenceName = sequence.SequenceName,
                obstacleGroups = new TutorialObstacleGroupJson[sequence.ObstacleGroups.Length]
            };

            for (int i = 0; i < sequence.ObstacleGroups.Length; i++)
            {
                json.obstacleGroups[i] = TutorialObstacleGroupJson.FromObstacleGroup(sequence.ObstacleGroups[i]);
            }

            return json;
        }
    }
} 