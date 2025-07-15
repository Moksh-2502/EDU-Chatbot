using UnityEngine;
using UnityEngine.AddressableAssets;
using SubwaySurfers.Tutorial.Events;

namespace SubwaySurfers.Tutorial.Data
{
    [System.Serializable]
    public class TutorialObstacleGroup
    {
        [Header("Obstacle Configuration")]
        public TutorialObstacleType obstacleType = TutorialObstacleType.TrashCan;
        public AssetReference obstaclePrefab;
        
        [Header("Spawn Configuration")]
        public int[] blockedLanes = { 1, 2 }; // Default: block center and right lanes
        public float spawnDistance = 15f;
        public int groupCount = 2;
        public float groupSeparation = 5f;
        
        public bool ValidateLanes()
        {
            foreach (int lane in blockedLanes)
            {
                if (lane < 0 || lane > 2) return false;
            }
            return true;
        }
    }
}