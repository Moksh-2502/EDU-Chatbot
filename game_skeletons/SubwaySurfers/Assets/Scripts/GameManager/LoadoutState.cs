using UnityEngine;
using UnityEngine.UI;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using Sounds;
using SubwaySurfers;
using TMPro;
using SubwaySurfers.UI.PreviewSystem;

/// <summary>
/// State pushed on the GameManager during the Loadout, when player select player, theme and accessories
/// Take care of init the UI, load all the data used for it etc.
/// </summary>
public class LoadoutState : AState
{
    [SerializeField] private CanvasGroup root;
    [Header("Character Preview System")]
    [SerializeField] private CharacterPreviewController characterPreviewController;

    [Header("Theme UI")] public TMP_Text themeNameDisplay;
    public RectTransform themeSelect;
    public Image themeIcon;

    public MissionUI missionPopup;
    public Button runButton;
    [SerializeField] private TMP_Text runButtonText;
    [SerializeField] private string defaultRunButtonText = "Run!";

    public MeshFilter skyMeshFilter;
    public MeshFilter UIGroundFilter;

    public AudioClip menuTheme;
    protected int m_UsedPowerupIndex;
    protected bool m_IsLoadingCharacter;

    protected Modifier m_CurrentModifier = new Modifier();

    protected const float k_CharacterRotationSpeed = 45f;
    protected const string k_ShopSceneName = "shop";
    protected const float k_OwnedAccessoriesCharacterOffset = -0.1f;
    protected int k_UILayer;
    protected readonly Quaternion k_FlippedYAxisRotation = Quaternion.Euler(0f, 180f, 0f);

    private bool _isPreparingWorld = false, _isInitialized = false;

    private TrackManager _trackManager;

    private void EnsureInitialized()
    {
        if (_isInitialized)
        {
            return;
        }
        _trackManager = FindFirstObjectByType<TrackManager>(FindObjectsInactive.Include);
        
        _isInitialized = true;
    }

    private void SetLoadingUIState(string btnTxt, bool interactable)
    {
        runButtonText.text = btnTxt;
        runButton.interactable = interactable;
    }
    private void AutoRefreshLoadingStatus()
    {
        if (_isPreparingWorld)
        {
            return;
        }
        bool isReady = CharacterDatabase.loaded && ThemeDatabase.loaded && characterPreviewController.IsVisible;
        string status = isReady ? defaultRunButtonText : "Loading...";
        SetLoadingUIState(status, isReady);
    }

    private void SetVisible(bool isVisible)
    {
        if (isVisible)
        {
            root.gameObject.SetActive(true);
        }

        root.DOFade(isVisible ? 1 : 0, 0.3f)
            .SetUpdate(true)
            .OnComplete(() =>
            {
                root.blocksRaycasts = root.interactable = isVisible;
                if (isVisible == false)
                {
                    root.gameObject.SetActive(false);
                }
            });
    }

    public override void Enter(AState from)
    {
        EnsureInitialized();
        _isPreparingWorld = false;

        SetVisible(true);
        missionPopup.gameObject.SetActive(false);

        themeNameDisplay.text = "";

        k_UILayer = LayerMask.NameToLayer("UI");

        skyMeshFilter.gameObject.SetActive(true);
        UIGroundFilter.gameObject.SetActive(true);

        // Reseting the global blinking value. Can happen if the game unexpectedly exited while still blinking
        Shader.SetGlobalFloat("_BlinkingValue", 0.0f);

        if (MusicPlayer.instance != null && MusicPlayer.instance.GetStem(0) != menuTheme)
        {
            MusicPlayer.instance.SetStem(0, menuTheme);
            StartCoroutine(MusicPlayer.instance.RestartAllStems());
        }

        AutoRefreshLoadingStatus();

        Refresh();
    }

    public override void Exit(AState to)
    {
        missionPopup.gameObject.SetActive(false);
        SetVisible(false);

        // Cleanup character preview
        if (characterPreviewController != null)
        {
            characterPreviewController.HidePreview();
        }

        GameState gs = to as GameState;

        skyMeshFilter.gameObject.SetActive(false);
        UIGroundFilter.gameObject.SetActive(false);

        if (gs != null)
        {
            gs.currentModifier = m_CurrentModifier;

            // We reset the modifier to a default one, for next run (if a new modifier is applied, it will replace this default one before the run starts)
            m_CurrentModifier = new Modifier();
        }
    }

    private async UniTaskVoid RefreshAsync()
    {
        try
        {
            await PopulateCharactersAsync();
            await PopulateThemeAsync();
        }
        catch (System.Exception ex)
        {
            Debug.LogException(ex);
        }
    }

    public void Refresh() => RefreshAsync().Forget();

    public override string GetName()
    {
        return "Loadout";
    }

    public override void Tick()
    {
        AutoRefreshLoadingStatus();
    }

    public void GoToStore()
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene(k_ShopSceneName,
            UnityEngine.SceneManagement.LoadSceneMode.Additive);
    }

    public void ChangeTheme(int dir)
    {
        ChangeThemeAsync(dir).Forget();
    }

    private async UniTaskVoid ChangeThemeAsync(int dir)
    {
        await IPlayerDataProvider.Instance.ChangeThemeAsync(dir);
        await PopulateThemeAsync();
    }

    private async UniTask PopulateThemeAsync()
    {
        await UniTask.WaitUntil(() => ThemeDatabase.loaded);
        ThemeData t = null;
        var data = await IPlayerDataProvider.Instance.GetAsync();
        while (t == null)
        {
            t = ThemeDatabase.GetThemeData(data.themes[data.usedTheme]);
            await UniTask.Yield();
        }

        themeNameDisplay.text = t.themeName;
        themeIcon.sprite = t.themeIcon;

        skyMeshFilter.sharedMesh = t.skyMesh;
        UIGroundFilter.sharedMesh = t.UIGroundMesh;
    }

    public async UniTask PopulateCharactersAsync()
    {
        if (characterPreviewController != null)
        {
            await PopulateCharactersWithPreviewSystemAsync();
        }
    }

    private async UniTask PopulateCharactersWithPreviewSystemAsync()
    {
        await UniTask.WaitUntil(() => CharacterDatabase.loaded);
        var data = await IPlayerDataProvider.Instance.GetAsync();
        Character c = CharacterDatabase.GetCharacter(data.characters[data.usedCharacter]);
        
        if (c == null)
        {
            Debug.LogWarning($"Character '{data.characters[data.usedCharacter]}' not found in database");
            return;
        }


        // Show character preview using the new system
        await characterPreviewController.ShowCharacterPreviewAsync(
            c);
    }

    public void SetModifier(Modifier modifier)
    {
        m_CurrentModifier = modifier;
    }

    private async UniTaskVoid StartGameAsync()
    {
        _isPreparingWorld = true;
        var data = await IPlayerDataProvider.Instance.GetAsync();
        if (data.ftueLevel == 1)
        {
            await IPlayerDataProvider.Instance.SetFTUELevelAsync(2);
        }
        
        SetLoadingUIState("Preparing...", false);

        try
        {
            // Prepare new game (handles isRerun = false and world preparation)
            if (_trackManager != null)
            {
                await _trackManager.PrepareGameAsync();
            }
            else
            {
                Debug.LogWarning("TrackManager instance not found! Falling back to standard flow.");
            }

            // World is now ready, transition to GameState for instant start
            manager.SwitchState("Game");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Failed to prepare world: {ex.Message}");
            
            SetLoadingUIState(defaultRunButtonText, true);
        }

        _isPreparingWorld = false;
    }

    public void StartGame()
    {
        StartGameAsync().Forget();
    }
}