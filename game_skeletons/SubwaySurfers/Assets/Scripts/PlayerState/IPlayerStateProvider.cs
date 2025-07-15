using System;

namespace Characters
{
    public interface IPlayerStateProvider
    {
        static IPlayerStateProvider Instance { get; private set; } = new PlayerStateProvider();
        event Action<int> OnLivesChanged;
        int CurrentLives { get; }
        int MaxLives { get; }

        int RunScore { get; }
        /// <summary>
        /// Coins picked this run, different from the coins owned by player in <see cref="PlayerData.coins"/>
        /// </summary>
        int RunCoins { get; }

        int TotalLostLives { get; }
        bool IsAlive => CurrentLives > 0;
        bool CanGainLives();
        bool ChangeLives(int amount);
        bool SetLives(int value, bool force = false);

        void ProcessPickedCoins(int amount);

        void Reset();
        void RegisterScoreMultiplier(IScoreMultiplier multiplier);
        void UnRegisterScoreMultiplier(IScoreMultiplier multiplier);
        float GetTotalMultiplier();
    }
}