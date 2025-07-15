using SharedCore.Analytics;

namespace SubwaySurfers.Analytics.Events.Gameplay
{
    /// <summary>
    /// Analytics event fired when the game ends (game over)
    /// </summary>
    public class GameOverEvent : BaseAnalyticsEvent
    {
        public override string EventName => "game_over";

        public string CharacterName { get; }
        public string ThemeName { get; }
        public float FinalDistance { get; }
        public int FinalScore { get; }
        public int CoinsCollected { get; }

        public GameOverEvent(string characterName, string themeName, float finalDistance, 
            int finalScore, int coinsCollected)
        {
            CharacterName = characterName;
            ThemeName = themeName;
            FinalDistance = finalDistance;
            FinalScore = finalScore;
            CoinsCollected = coinsCollected;
        }
    }
} 