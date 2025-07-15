using System;
using System.Collections.Generic;
using Characters;
using Cysharp.Threading.Tasks;
using SubwaySurfers;
using UnityEngine;

public class PlayerStateProvider : IPlayerStateProvider
{
    public event Action<int> OnLivesChanged;
    public int CurrentLives { get; private set; }
    public int MaxLives { get; }

    public int RunScore { get; private set; }

    public int RunCoins { get; private set; }

    public int TotalLostLives { get; private set; }

    private readonly CharacterConfig _config;

    private readonly HashSet<IScoreMultiplier> _scoreMultipliers = new HashSet<IScoreMultiplier>();

    public PlayerStateProvider()
    {
        _config = Resources.Load<CharacterConfig>("CharacterConfig");
        CurrentLives = _config.StartingLives;
        MaxLives = _config.MaxLives;
    }

    public bool SetLives(int newLives, bool force)
    {
        if (force == false && newLives == CurrentLives)
        {
            return false; // No change needed
        }

        if (newLives < 0 || newLives > MaxLives)
        {
            return false;
        }

        CurrentLives = newLives;
        OnLivesChanged?.Invoke(CurrentLives);
        return true;
    }

    public bool ChangeLives(int amount)
    {
        if (amount == 0) return false;
        var prevLives = CurrentLives;
        var changed = SetLives(Mathf.Clamp(CurrentLives + amount, 0, MaxLives), false);

        var livesLost = prevLives > CurrentLives ? prevLives - CurrentLives : 0;
        RunScore = Mathf.Clamp(RunScore - livesLost * 10, 0, int.MaxValue);
        TotalLostLives += livesLost;
        return changed;
    }

    public bool CanGainLives() => CurrentLives < MaxLives;

    public void ProcessPickedCoins(int coins)
    {
        var totalMultiplier = IPlayerStateProvider.Instance.GetTotalMultiplier();
        RunScore += Mathf.RoundToInt(coins * totalMultiplier);
        RunCoins += coins;
        IPlayerDataProvider.Instance.AddCoinsAsync(coins).Forget();
    }

    public void Reset()
    {
        CurrentLives = _config.StartingLives;
        RunScore = 0;
        RunCoins = 0;
        OnLivesChanged?.Invoke(CurrentLives);
        Debug.Log("Player lives reset to starting value.");
    }

    public void RegisterScoreMultiplier(IScoreMultiplier multiplier)
    {
        _scoreMultipliers.Add(multiplier);
    }

    public void UnRegisterScoreMultiplier(IScoreMultiplier multiplier)
    {
        _scoreMultipliers.Remove(multiplier);
    }

    public float GetTotalMultiplier()
    {
        float value = 0;
        foreach (var multiplier in _scoreMultipliers)
        {
            if (multiplier == null)
            {
                continue;
            }

            value += multiplier.GetMultiplier();
        }

        return value;
    }
}