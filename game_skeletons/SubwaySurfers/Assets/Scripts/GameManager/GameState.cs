using System;
using UnityEngine;
using UnityEngine.UI;
using SubwaySurfers;
using Cysharp.Threading.Tasks;
using ReusablePatterns.FluencySDK.Scripts.Interfaces;
using ReusablePatterns.FluencySDK.Enums;
using Sounds;
using Characters;
using SubwaySurfers.Tutorial.Core;
using ReusablePatterns.SharedCore.Runtime.PauseSystem;

public interface IGameState
{
    event Action OnGameStarted;
    event Action OnGameFinished;
    event Action OnRunEnd;
    event Action OnSecondWindRequested;
    event Action OnSecondWindStarted;
    bool IsFinished { get; }
    void SecondWind();
    void GameOver();
    void RequestSecondWind();
}

/// <summary>
/// Pushed on top of the GameManager during gameplay. Takes care of initializing all the UI and start the TrackManager
/// Also will take care of cleaning when leaving that state.
/// </summary>
public class GameState : AState, ISDKTimeProvider, IGameState
{
    private static int s_StartHash = Animator.StringToHash("Start");

    public Canvas canvas;
    public TrackManager trackManager;

    public AudioClip gameTheme;

    [Header("UI")] public Text coinText;
    public Text scoreText;
    public Text distanceText;
    public Text multiplierText;

    public Image inventoryIcon;

    public Modifier currentModifier = new Modifier();

    public bool IsFinished { get; private set; }

    public QuestionGenerationMode QuestionMode { get; private set; }

    public double StartTime { get; private set; }

    public event Action OnTimeRestarted;
    public event Action OnGameStarted;
    public event Action OnGameFinished;
    public event Action OnRunEnd;
    public event Action OnSecondWindRequested;
    public event Action OnSecondWindStarted;

    private IGamePauser _gamePauser;
    private bool _initialized = false;

    private void Awake()
    {
        EnsureInitialized();
    }

    private void EnsureInitialized()
    {
        if (_initialized)
        {
            return;
        }

        _initialized = true;
        _gamePauser = FindFirstObjectByType<PauseManager>(FindObjectsInactive.Include);
        trackManager.characterController.characterCollider.OnDeath += OnCharacterDeath;
    }

    public override void Enter(AState from)
    {
        if (MusicPlayer.instance.GetStem(0) != gameTheme)
        {
            MusicPlayer.instance.SetStem(0, gameTheme);
            CoroutineHandler.StartStaticCoroutine(MusicPlayer.instance.RestartAllStems());
        }

        StartGame();
    }

    public override void Exit(AState to)
    {
        canvas.gameObject.SetActive(false);

        ClearPowerup();
    }

    private async UniTaskVoid StartGameAsync()
    {
        try
        {
            canvas.gameObject.SetActive(true);

            currentModifier.OnRunStart(this);

            IsFinished = false;

            await trackManager.PrepareGameAsync();
            trackManager.characterController.gameObject.SetActive(true);

            trackManager.characterController.Begin();
            await WaitToStart();
            QuestionMode = QuestionGenerationMode.BreakBased;
            StartTime = Time.realtimeSinceStartupAsDouble;
            if (ITutorialManager.Instance != null)
            {
                await ITutorialManager.Instance.StartTutorialAsync(forceStart: false);
            }

            OnTimeRestarted?.Invoke();
            OnGameStarted?.Invoke();
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
        }
    }

    private async UniTask WaitToStart()
    {
        trackManager.characterController.character.animator.Play(s_StartHash);
        _gamePauser.Pause(new PauseData()
        {
            resumeWithCountdown = true,
            animateCharacter = true,
            displayMenu = false,
            ignoreGameState = true,
        });

        _gamePauser.Resume();
        await UniTask.WaitUntil(() => _gamePauser.IsPaused == false);

        if (trackManager.isRerun)
        {
            // Make invincible on rerun, to avoid problems if the character died in front of an obstacle
            trackManager.characterController.characterCollider.SetInvincible();
        }

        trackManager.characterController.StartRunning();
        trackManager.StartMove();
    }

    public void StartGame()
    {
        StartGameAsync().Forget();
    }

    public override string GetName()
    {
        return "Game";
    }

    public override void Tick()
    {
        if (IsFinished)
        {
            return;
        }

        if (trackManager.isLoaded)
        {
            UpdateUI();

            currentModifier.OnRunTick(this);
        }
    }

    public void QuitToLoadout()
    {
        trackManager.End();
        trackManager.isRerun = false;
        IPlayerDataProvider.Instance.SaveAsync().Forget();
        manager.SwitchState("Loadout");
    }

    protected void UpdateUI()
    {
        coinText.text = IPlayerStateProvider.Instance.RunCoins.ToString();

        scoreText.text = IPlayerStateProvider.Instance.RunScore.ToString();
        multiplierText.text = $"x{IPlayerStateProvider.Instance.GetTotalMultiplier():F0}";
        distanceText.text = Mathf.FloorToInt(trackManager.worldDistance) + "m";

        // Consumable
        if (trackManager.characterController.inventory != null)
        {
            inventoryIcon.transform.parent.gameObject.SetActive(true);
            inventoryIcon.sprite = trackManager.characterController.inventory.icon;
        }
        else
            inventoryIcon.transform.parent.gameObject.SetActive(false);
    }

    private void OnCharacterDeath(DeathEventArgs args)
    {
        ProcessCharacterDeathAsync().Forget();
    }

    private async UniTaskVoid ProcessCharacterDeathAsync()
    {
        IsFinished = true;
        QuestionMode = QuestionGenerationMode.None;
        trackManager.StopMove();
        OnGameFinished?.Invoke();

        // Reseting the global blinking value. Can happen if game unexpectly exited while still blinking
        Shader.SetGlobalFloat("_BlinkingValue", 0.0f);

        await UniTask.WaitForSeconds(2, ignoreTimeScale: true);
        if (currentModifier.OnRunEnd(this))
        {
            OnRunEnd?.Invoke();
            ClearPowerup();
        }
    }

    protected void ClearPowerup()
    {
        trackManager.characterController.powerupSource.Stop();
    }

    public void GameOver()
    {
        manager.SwitchState("GameOver");
    }

    public void SecondWind()
    {
        IPlayerStateProvider.Instance.SetLives(1);
        trackManager.isRerun = true;
        StartGame();
        OnSecondWindStarted?.Invoke();
    }

    public void RequestSecondWind()
    {
        QuestionMode = QuestionGenerationMode.NoBreaks;
        OnSecondWindRequested?.Invoke();
    }
}