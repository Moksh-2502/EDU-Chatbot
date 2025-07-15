using SharedCore.Analytics;

namespace SubwaySurfers.Analytics.Events.Gameplay
{
    /// <summary>
    /// Analytics event fired when player loses a life due to collision
    /// </summary>
    public class CollisionLifeLossEvent : BaseAnalyticsEvent
    {
        public override string EventName => "collision_life_loss";

        public string CharacterName { get; }
        public string ThemeName { get; }
        public string ObstacleType { get; }
        public float DistanceTraveled { get; }
        public int Score { get; }
        public int CoinsCollected { get; }
        public int LivesRemaining { get; }

        public CollisionLifeLossEvent(string characterName, string themeName, string obstacleType, 
            float distanceTraveled, int score, int coinsCollected, int livesRemaining)
        {
            CharacterName = characterName;
            ThemeName = themeName;
            ObstacleType = obstacleType;
            DistanceTraveled = distanceTraveled;
            Score = score;
            CoinsCollected = coinsCollected;
            LivesRemaining = livesRemaining;
        }
    }
} 