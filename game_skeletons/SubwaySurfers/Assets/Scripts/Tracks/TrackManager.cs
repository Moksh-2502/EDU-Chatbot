using System;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Characters;
using Cysharp.Threading.Tasks;
using Data;
using Sounds;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using GameObject = UnityEngine.GameObject;
using Random = UnityEngine.Random;
using SubwaySurfers;
using SubwaySurfers.Assets.Scripts.Tracks;

/// <summary>
/// The TrackManager handles creating track segments, moving them and handling the whole pace of the game.
/// 
/// The cycle is as follows:
/// - Begin is called when the game starts.
///     - if it's a first run, init the controller, collider etc. and start the movement of the track.
///     - if it's a rerun (after watching ads on GameOver) just restart the movement of the track.
/// - Update moves the character and - if the character reaches a certain distance from origin (given by floatingOriginThreshold) -
/// moves everything back by that threshold to "reset" the player to the origin. This allow to avoid floating point error on long run.
/// It also handles creating the tracks segements when needed.
/// 
/// If the player has no more lives, it pushes the GameOver state on top of the GameState without removing it. That way we can just go back to where
/// we left off if the player watches an ad and gets a second chance. If the player quits, then:
/// 
/// - End is called and everything is cleared and destroyed, and we go back to the Loadout State.
/// </summary>
public class TrackManager : MonoBehaviour, IScoreMultiplier
{
    private enum PreparationState
    {
        None,
        InProgress,
        Done,
    }

    static public TrackManager instance
    {
        get { return s_Instance; }
    }

    static protected TrackManager s_Instance;


    [Header("Debug")] [Tooltip("Enable debug logging for track segment management")]
    public bool enableSegmentDebugLogs = false;

    [Header("Character & Movements")] public CharacterInputController characterController;


    // Legacy fields - kept for backward compatibility but will be overridden by configProvider
    public float laneOffset = 1.0f;

    public bool invincible = false;

    public MeshFilter skyMeshFilter;

    [Header("Obstacle Spawning Validation")]
    [Tooltip("Radius for physics check before spawning obstacles")]
    [SerializeField]
    private float obstacleCheckRadius = 1.5f;

    [Tooltip("Layer mask for obstacle collision detection")] [SerializeField]
    private LayerMask obstacleCheckLayerMask = -1;

    [Tooltip("Number of segments near player to protect from density changes")] [SerializeField]
    private int protectedSegmentCount = 2, normalSegmentCount = 10;

    [Header("Parallax")] public Transform parallaxRoot;
    public float parallaxRatio = 0.5f;

    public event System.Action<TrackSegment> newSegmentCreated;

    public int trackSeed
    {
        get { return m_TrackSeed; }
        set { m_TrackSeed = value; }
    }

    public float currentSegmentDistance
    {
        get { return m_CurrentSegmentDistance; }
    }

    public float worldDistance
    {
        get { return m_TotalWorldDistance; }
    }

    public float speed { get; private set; }

    public float speedRatio
    {
        get
        {
            return (speed - ITrackRunnerConfigProvider.Instance.MinSpeed) /
                   (ITrackRunnerConfigProvider.Instance.MaxSpeed - ITrackRunnerConfigProvider.Instance.MinSpeed);
        }
    }

    public int currentZone
    {
        get { return m_CurrentZone; }
    }

    public TrackSegment currentSegment => segments.Count > 0 ? segments[0] : null;

    public readonly List<TrackSegment> segments = new List<TrackSegment>(8);

    public ThemeData currentTheme
    {
        get { return m_CurrentThemeData; }
    }

    public bool isMoving
    {
        get { return m_IsMoving; }
    }

    public bool isRerun
    {
        get { return m_Rerun; }
        set { m_Rerun = value; }
    }

    public bool isLoaded { get; set; }

    // If this is set to -1, random seed is init to system clock, otherwise init to that value
    // Allow to play the same game multiple time (useful to make specific competition/challenge fair between players)
    protected int m_TrackSeed = -1;

    protected float m_CurrentSegmentDistance;
    protected float m_TotalWorldDistance;
    protected bool m_IsMoving;

    protected List<TrackSegment> m_PastSegments = new List<TrackSegment>();
    protected int m_SafeSegementLeft;

    protected ThemeData m_CurrentThemeData;
    protected int m_CurrentZone;
    protected float m_CurrentZoneDistance;
    protected int m_PreviousSegment = -1;

    // Virtual Origin System - replaces world shifting
    private Vector3 m_VirtualOrigin = Vector3.zero;
    private float m_VirtualOriginDistance = 0f;

    // Track segment lifecycle with distances
    private readonly Dictionary<TrackSegment, float> m_SegmentSpawnDistances = new Dictionary<TrackSegment, float>();
    private readonly Dictionary<TrackSegment, float> m_SegmentPassDistances = new Dictionary<TrackSegment, float>();

    protected bool
        m_Rerun; // This lets us know if we are entering a game over (ads) state or starting a new game (see GameState)

    const float k_FloatingOriginThreshold = 10000f;
    protected const float k_StartingSegmentDistance = 2f;
    protected const int k_StartingSafeSegments = 2;
    protected const int k_StartingCoinPoolSize = 256;
    protected const float k_SegmentRemovalDistance = -30f;


    private IGameManager _gameManager;

    private float m_LastKnownObstacleDensity = -1f;

    private bool _isInitialized = false;

    private int _parallaxRootChildren = 0;
    private int _pendingSpawns = 0;
    private int _pendingCloudSpawns = 0; // Track async cloud operations

    private readonly Vector3 _offScreenSpawnPos = new Vector3(-100f, -100f, -100f);

    private PreparationState _preparationState = PreparationState.None;

    private CancellationTokenSource _tickCts = null;

    protected void Awake()
    {
        Initialize();
    }

    private void OnEnable()
    {
        IPlayerStateProvider.Instance.RegisterScoreMultiplier(this);
        StartTicking();
    }

    private void OnDisable()
    {
        IPlayerStateProvider.Instance.UnRegisterScoreMultiplier(this);
        StopTicking();
    }

    private void Initialize()
    {
        if (_isInitialized)
        {
            return;
        }

        _isInitialized = true;
        s_Instance = this;

        _gameManager = FindFirstObjectByType<GameManager>(FindObjectsInactive.Include);
        _gameManager.OnGameStateChanged += OnGameStateChanged;
    }

    private void StartTicking()
    {
        _tickCts?.Cancel();
        _tickCts = new CancellationTokenSource();
        TickAsync(_tickCts.Token).Forget();
    }

    private void StopTicking()
    {
        _tickCts?.Cancel();
    }

    private void OnGameStateChanged(AState state)
    {
        if (state is not GameState)
        {
            _preparationState = PreparationState.None;
            isLoaded = false;
        }
    }

    public void StartMove(bool isRestart = true)
    {
        characterController.StartMoving();
        m_IsMoving = true;
        if (isRestart)
        {
            speed = ITrackRunnerConfigProvider.Instance.MinSpeed;
            Debug.Log("TrackManager: Starting movement with speed: " + speed);
        }
    }

    public void StopMove()
    {
        m_IsMoving = false;
    }

    /// <summary>
    /// Initializes theme and environment settings
    /// </summary>
    private void InitializeThemeAndEnvironment(PlayerData playerData)
    {
        m_CurrentThemeData = ThemeDatabase.GetThemeData(playerData.themes[playerData.usedTheme]);

        m_CurrentZone = 0;
        m_CurrentZoneDistance = 0;

        skyMeshFilter.sharedMesh = m_CurrentThemeData.skyMesh;
        RenderSettings.fogColor = m_CurrentThemeData.fogColor;
        RenderSettings.fog = true;
    }

    /// <summary>
    /// Instantly begins gameplay - world should already be prepared via PrepareWorld()
    /// </summary>
    public async UniTask PrepareGameAsync()
    {
        if (_preparationState != PreparationState.None)
        {
            await UniTask.WaitUntil(() => _preparationState == PreparationState.Done);
            return;
        }

        // Start new preparation if none is in progress
        _preparationState = PreparationState.InProgress;
        Initialize();
        var playerData = await IPlayerDataProvider.Instance.GetAsync();

        if (!m_Rerun)
        {
            if (m_TrackSeed != -1)
                Random.InitState(m_TrackSeed);
            else
                Random.InitState((int)System.DateTime.Now.Ticks);

            // Since this is not a rerun, init the whole system (on rerun we want to keep the states we had on death)
            m_CurrentSegmentDistance = k_StartingSegmentDistance;
            m_TotalWorldDistance = 0.0f;

            // Keep character controller inactive during world preparation
            characterController.gameObject.SetActive(false);

            //Addressables 1.0.1-preview
            // Spawn the player
            var op = Addressables.InstantiateAsync(playerData.characters[playerData.usedCharacter],
                Vector3.zero,
                Quaternion.identity);
            await op;

            if (op.Result == null || !(op.Result is GameObject))
            {
                Debug.LogWarning(string.Format("Unable to load character {0}.",
                    playerData.characters[playerData.usedCharacter]));
                return;
            }

            Character player = op.Result.GetComponent<Character>();

            characterController.character = player;
            characterController.trackManager = this;

            characterController.Init();
            characterController.CheatInvincible(invincible);

            player.transform.SetParent(characterController.characterCollider.transform, false);

            InitializeThemeAndEnvironment(playerData);

            gameObject.SetActive(true);

            m_SafeSegementLeft = k_StartingSafeSegments;
            Coin.coinPool = new Pooler(currentTheme.collectiblePrefab, k_StartingCoinPoolSize);
            playerData.StartRunMissions(this);

            // Pre-spawn initial segments and clouds
            await PreSpawnWorldElements();

            if (enableSegmentDebugLogs)
                Debug.Log(
                    $"[TRACK_DEBUG] World preparation completed: Segments={segments.Count}, Clouds={_parallaxRootChildren}");
        }
        else
        {
            // For reruns, world is already prepared, just ensure character controller is ready
            characterController.gameObject.SetActive(false);
            if (enableSegmentDebugLogs)
                Debug.Log("[TRACK_DEBUG] Rerun - skipping world preparation, world already ready");
        }

        isLoaded = true;
        _preparationState = PreparationState.Done;
    }

    /// <summary>
    /// Pre-spawns initial world elements to prevent visual discrepancy
    /// </summary>
    private async UniTask PreSpawnWorldElements()
    {
        int targetSegmentCount = normalSegmentCount;
        int targetCloudCount = currentTheme.cloudNumber;

        // Pre-spawn initial track segments using existing spawning logic
        var segmentTasks = new List<UniTask>();
        for (int i = 0; i < Mathf.Min(targetSegmentCount, 3); i++) // Spawn at least 3 initial segments
        {
            segmentTasks.Add(SpawnSegmentAsync(isPreparation: true));
        }

        // Pre-spawn initial clouds using existing spawning logic
        var cloudTasks = new List<UniTask>();
        for (int i = 0; i < Mathf.Min(targetCloudCount, 5); i++) // Spawn at least 5 initial clouds
        {
            cloudTasks.Add(SpawnSingleCloudAsyncWithTracking());
        }

        // Wait for all initial spawns to complete
        var allTasks = segmentTasks.Concat(cloudTasks);
        await UniTask.WhenAll(allTasks);

        if (enableSegmentDebugLogs)
            Debug.Log($"[TRACK_DEBUG] Pre-spawn completed: Segments={segments.Count}, Clouds={_parallaxRootChildren}");
    }

    /// <summary>
    /// Core segment spawning logic - unified for both preparation and runtime spawning
    /// </summary>
    private async UniTask<TrackSegment> SpawnSegmentAsync(bool isPreparation = false)
    {
        // Zone management
        if (m_CurrentThemeData.zones[m_CurrentZone].length < m_CurrentZoneDistance)
        {
            ChangeZone();
        }

        // Segment selection
        int segmentUse = Random.Range(0, m_CurrentThemeData.zones[m_CurrentZone].prefabList.Length);
        if (segmentUse == m_PreviousSegment)
            segmentUse = (segmentUse + 1) % m_CurrentThemeData.zones[m_CurrentZone].prefabList.Length;

        // Instantiate segment
        AsyncOperationHandle segmentToUseOp = m_CurrentThemeData.zones[m_CurrentZone].prefabList[segmentUse]
            .InstantiateAsync(_offScreenSpawnPos, Quaternion.identity);
        await segmentToUseOp;

        if (segmentToUseOp.Result == null || !(segmentToUseOp.Result is GameObject))
        {
            Debug.LogWarning(
                $"Unable to load segment {m_CurrentThemeData.zones[m_CurrentZone].prefabList[segmentUse].Asset.name}.");
            return null;
        }

        TrackSegment newSegment = (segmentToUseOp.Result as GameObject).GetComponent<TrackSegment>();
        ConfigureSegment(newSegment);

        // Handle obstacles
        await HandleSegmentObstacles(newSegment);

        newSegment.IsReady = true;

        if (enableSegmentDebugLogs)
            Debug.Log($"[TRACK_DEBUG] Segment spawned: Name={newSegment.name}, " +
                      $"WorldLength={newSegment.worldLength:F2}, TotalSegments={segments.Count}, " +
                      $"IsPreparation={isPreparation}");

        return newSegment;
    }

    /// <summary>
    /// Configures a newly spawned segment with position, rotation, and scale
    /// </summary>
    private void ConfigureSegment(TrackSegment newSegment)
    {
        // Get exit point from last segment or use transform position
        Vector3 currentExitPoint;
        Quaternion currentExitRotation;
        if (segments.Count > 0)
        {
            segments[^1].GetPointAt(1.0f, out currentExitPoint, out currentExitRotation);
        }
        else
        {
            currentExitPoint = transform.position;
            currentExitRotation = transform.rotation;
        }

        // Position and orient the segment
        newSegment.transform.rotation = currentExitRotation;
        newSegment.GetPointAt(0.0f, out var entryPoint, out _);
        Vector3 pos = currentExitPoint + (newSegment.transform.position - entryPoint);
        newSegment.transform.position = pos;
        newSegment.manager = this;

        // Apply random horizontal flip
        newSegment.transform.localScale = new Vector3((Random.value > 0.5f ? -1 : 1), 1, 1);
        newSegment.objectRoot.localScale = new Vector3(1.0f / newSegment.transform.localScale.x, 1, 1);

        // Add to segments list and track spawn distance
        segments.Add(newSegment);
        m_SegmentSpawnDistances[newSegment] = m_TotalWorldDistance + m_VirtualOriginDistance;

        newSegmentCreated?.Invoke(newSegment);
    }

    /// <summary>
    /// Handles obstacle spawning for a segment based on current game state
    /// </summary>
    private async UniTask HandleSegmentObstacles(TrackSegment segment)
    {
        if (m_SafeSegementLeft <= 0)
        {
            if (ShouldSpawnObstaclesOnSegment())
            {
                await SpawnObstacle(segment);
            }
        }
        else
        {
            m_SafeSegementLeft -= 1;
        }
    }

    /// <summary>
    /// Spawns a new segment asynchronously with tracking
    /// </summary>
    private async UniTaskVoid SpawnNewSegmentAsync()
    {
        try
        {
            await SpawnSegmentAsync(isPreparation: false);
        }
        finally
        {
            _pendingSpawns--;
        }
    }

    /// <summary>
    /// Spawns a single cloud asynchronously with proper tracking
    /// </summary>
    private async UniTask SpawnSingleCloudAsyncWithTracking()
    {
        try
        {
            await SpawnSingleCloudAsync();
        }
        finally
        {
            _pendingCloudSpawns--;
        }
    }

    /// <summary>
    /// Spawns a single cloud using Unity's InstantiateAsync API
    /// </summary>
    private async UniTask SpawnSingleCloudAsync()
    {
        if (parallaxRoot == null || currentTheme.cloudPrefabs.Length == 0)
            return;

        float lastZ = GetLastCloudZ();
        GameObject cloudPrefab = GetRandomCloudPrefab();

        if (cloudPrefab != null)
        {
            var handle = InstantiateAsync(cloudPrefab);
            await handle;

            if (handle.Result is { Length: > 0 })
            {
                ConfigureCloud(handle.Result[0], lastZ);
            }
        }
    }

    /// <summary>
    /// Gets the Z position of the last spawned cloud
    /// </summary>
    private float GetLastCloudZ()
    {
        return parallaxRoot.childCount == 0
            ? 0
            : parallaxRoot.GetChild(parallaxRoot.childCount - 1).position.z + currentTheme.cloudMinimumDistance.z;
    }

    /// <summary>
    /// Gets a random cloud prefab from the current theme
    /// </summary>
    private GameObject GetRandomCloudPrefab()
    {
        return currentTheme.cloudPrefabs[Random.Range(0, currentTheme.cloudPrefabs.Length)];
    }

    /// <summary>
    /// Configures a newly spawned cloud with position, scale, and rotation
    /// </summary>
    private void ConfigureCloud(GameObject cloudObj, float lastZ)
    {
        cloudObj.transform.SetParent(parallaxRoot, false);

        cloudObj.transform.localPosition =
            Vector3.up * (currentTheme.cloudMinimumDistance.y + (Random.value - 0.5f) * currentTheme.cloudSpread.y)
            + Vector3.forward * (lastZ + (Random.value - 0.5f) * currentTheme.cloudSpread.z)
            + Vector3.right *
            (currentTheme.cloudMinimumDistance.x + (Random.value - 0.5f) * currentTheme.cloudSpread.x);

        cloudObj.transform.localScale *= 1.0f + (Random.value - 0.5f) * 0.5f;
        cloudObj.transform.localRotation = Quaternion.AngleAxis(Random.value * 360.0f, Vector3.up);

        _parallaxRootChildren++;
    }

    public void End()
    {
        CleanupSegmentCollections();
        CleanupVirtualOriginTracking();
        CleanupCharacterController();
        CleanupParallaxObjects();
        CleanupInventory();
    }

    /// <summary>
    /// Cleans up all segment collections
    /// </summary>
    private void CleanupSegmentCollections()
    {
        CleanupSegmentList(segments);
        CleanupSegmentList(m_PastSegments);
        segments.Clear();
        m_PastSegments.Clear();
        _pendingSpawns = 0;
    }

    /// <summary>
    /// Cleans up a list of segments safely
    /// </summary>
    private void CleanupSegmentList(List<TrackSegment> segmentList)
    {
        foreach (var seg in segmentList)
        {
            if (seg?.gameObject != null)
                seg.Cleanup();
        }
    }

    /// <summary>
    /// Cleans up virtual origin tracking data
    /// </summary>
    private void CleanupVirtualOriginTracking()
    {
        m_SegmentSpawnDistances.Clear();
        m_SegmentPassDistances.Clear();
        m_VirtualOrigin = Vector3.zero;
        m_VirtualOriginDistance = 0f;
    }

    /// <summary>
    /// Cleans up character controller related objects
    /// </summary>
    private void CleanupCharacterController()
    {
        characterController.End();
        gameObject.SetActive(false);

        if (characterController.character != null)
        {
            Addressables.ReleaseInstance(characterController.character.gameObject);
            characterController.character = null;
        }

        characterController.gameObject.SetActive(false);
    }

    /// <summary>
    /// Cleans up parallax objects (clouds)
    /// </summary>
    private void CleanupParallaxObjects()
    {
        for (int i = 0; i < parallaxRoot.childCount; ++i)
        {
            _parallaxRootChildren--;
            Destroy(parallaxRoot.GetChild(i).gameObject);
        }
    }

    /// <summary>
    /// Cleans up inventory if needed
    /// </summary>
    private void CleanupInventory()
    {
        if (characterController.inventory != null)
        {
            IPlayerDataProvider.Instance.AddConsumableAsync(characterController.inventory.GetConsumableType())
                .ContinueWith(() => { characterController.inventory = null; });
        }
    }

    private async UniTaskVoid TickAsync(CancellationToken cancellationToken)
    {
        try
        {
            while (cancellationToken.IsCancellationRequested == false)
            {
                await UniTask.Yield(cancellationToken);
                cancellationToken.ThrowIfCancellationRequested();
                HandleSegmentSpawning();
                HandleCloudSpawning();

                if (!m_IsMoving)
                    continue;

                UpdateGameMetrics();
                HandleSegmentProgression();
                UpdateCharacterTransform();
                HandleParallaxAndClouds();
                HandleSegmentCleanup();
                UpdateGameSpeed();
                await UpdatePlayerRankAndMissionsAsync();
                cancellationToken.ThrowIfCancellationRequested();
                MusicPlayer.instance.UpdateVolumes(speedRatio);
                HandleObstacleDensityChanges();
            }
        }
        catch (OperationCanceledException)
        {
            
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
        }
    }

    /// <summary>
    /// Handles spawning new segments when needed
    /// </summary>
    private void HandleSegmentSpawning()
    {
        int targetSegmentCount = normalSegmentCount;
        while (segments.Count + _pendingSpawns < targetSegmentCount)
        {
            if (enableSegmentDebugLogs)
                Debug.Log($"[TRACK_DEBUG] Spawning new segment: ActiveSegments={segments.Count}, " +
                          $"PendingSpawns={_pendingSpawns}, Target={targetSegmentCount}");
            SpawnNewSegmentAsync().Forget();
            _pendingSpawns++;
        }
    }

    /// <summary>
    /// Handles spawning new clouds when needed
    /// </summary>
    private void HandleCloudSpawning()
    {
        if (parallaxRoot != null && currentTheme.cloudPrefabs.Length > 0)
        {
            int totalClouds = _parallaxRootChildren + _pendingCloudSpawns;
            if (totalClouds < currentTheme.cloudNumber)
            {
                _pendingCloudSpawns++;
                SpawnSingleCloudAsyncWithTracking().Forget();
            }
        }
    }

    /// <summary>
    /// Updates game metrics like score and distance
    /// </summary>
    private void UpdateGameMetrics()
    {
        float scaledSpeed = speed * Time.deltaTime;
        m_CurrentZoneDistance += scaledSpeed;
        m_TotalWorldDistance += scaledSpeed;
        m_CurrentSegmentDistance += scaledSpeed;
    }

    /// <summary>
    /// Handles progression through segments
    /// </summary>
    private void HandleSegmentProgression()
    {
        if (segments.Count == 0)
            return;

        if (m_CurrentSegmentDistance > segments[0].worldLength)
        {
            ProgressToNextSegment();
        }
    }

    /// <summary>
    /// Progresses to the next segment in the sequence
    /// </summary>
    private void ProgressToNextSegment()
    {
        if (enableSegmentDebugLogs)
            Debug.Log($"[TRACK_DEBUG] Moving segment to past: CurrentSegmentDistance={m_CurrentSegmentDistance:F2}, " +
                      $"SegmentWorldLength={segments[0].worldLength:F2}, SegmentName={segments[0].name}");

        m_CurrentSegmentDistance -= segments[0].worldLength;

        // Track when this segment was passed for cleanup logic
        TrackSegment passedSegment = segments[0];
        m_SegmentPassDistances[passedSegment] = m_TotalWorldDistance + m_VirtualOriginDistance;

        // Move segment to past segments collection
        m_PastSegments.Add(passedSegment);
        segments.RemoveAt(0);
    }

    /// <summary>
    /// Updates character transform based on segment progression
    /// </summary>
    private void UpdateCharacterTransform()
    {
        if (segments.Count == 0)
            return;

        Vector3 currentPos;
        Quaternion currentRot;
        Transform characterTransform = characterController.transform;

        segments[0].GetPointAtInWorldUnit(m_CurrentSegmentDistance, out currentPos, out currentRot);

        // Virtual Origin System - maintains precision without moving objects
        UpdateVirtualOrigin();

        // Parallax Handling - simplified without world shifting
        HandleParallaxMovement(currentPos, characterTransform);

        characterTransform.rotation = currentRot;
        characterTransform.position = currentPos;
    }

    /// <summary>
    /// Handles parallax movement for background elements
    /// </summary>
    private void HandleParallaxMovement(Vector3 currentPos, Transform characterTransform)
    {
        if (parallaxRoot != null)
        {
            Vector3 difference = (currentPos - characterTransform.position) * parallaxRatio;
            int count = parallaxRoot.childCount;
            for (int i = 0; i < count; i++)
            {
                Transform cloud = parallaxRoot.GetChild(i);
                cloud.position += difference;
            }
        }
    }

    /// <summary>
    /// Handles parallax objects and cloud cleanup
    /// </summary>
    private void HandleParallaxAndClouds()
    {
        if (parallaxRoot == null || currentTheme.cloudPrefabs.Length == 0)
            return;

        Vector3 currentPos = characterController.transform.position;

        for (int i = parallaxRoot.childCount - 1; i >= 0; i--)
        {
            Transform child = parallaxRoot.GetChild(i);
            if ((child.localPosition - currentPos).z < -50)
            {
                _parallaxRootChildren--;
                Destroy(child.gameObject);
            }
        }
    }

    /// <summary>
    /// Handles cleanup of past segments
    /// </summary>
    private void HandleSegmentCleanup()
    {
        for (int i = m_PastSegments.Count - 1; i >= 0; i--)
        {
            TrackSegment segment = m_PastSegments[i];
            if (ShouldCleanupSegment(segment))
            {
                if (enableSegmentDebugLogs)
                {
                    float passDistance = m_SegmentPassDistances.TryGetValue(segment, out float value) ? value : 0f;
                    float currentTotalDistance = m_TotalWorldDistance + m_VirtualOriginDistance;
                    Debug.Log($"[TRACK_DEBUG] Cleaning up past segment: Name={segment.name}, " +
                              $"DistanceSincePass={currentTotalDistance - passDistance:F2}");
                }

                segment.Cleanup();
                m_PastSegments.RemoveAt(i);

                // Clean up tracking dictionaries
                m_SegmentSpawnDistances.Remove(segment);
                m_SegmentPassDistances.Remove(segment);
            }
        }
    }

    /// <summary>
    /// Updates game speed progression
    /// </summary>
    private void UpdateGameSpeed()
    {
        speed = Mathf.Clamp(speed + ITrackRunnerConfigProvider.Instance.Acceleration * Time.deltaTime,
            ITrackRunnerConfigProvider.Instance.MinSpeed, ITrackRunnerConfigProvider.Instance.MaxSpeed);
    }

    /// <summary>
    /// Updates player rank and missions
    /// </summary>
    private async UniTask UpdatePlayerRankAndMissionsAsync()
    {
        var playerData = await IPlayerDataProvider.Instance.GetAsync();

        // Check for next rank achieved
        int currentTarget = (playerData.rank + 1) * 300;
        if (m_TotalWorldDistance > currentTarget)
        {
            playerData.rank += 1;
            IPlayerDataProvider.Instance.SaveAsync().Forget();
        }

        playerData.UpdateMissions(this);
    }

    /// <summary>
    /// Handles obstacle density changes
    /// </summary>
    private void HandleObstacleDensityChanges()
    {
        float currentDensity = ITrackRunnerConfigProvider.Instance.ObstacleDensity;
        if (!Mathf.Approximately(currentDensity, m_LastKnownObstacleDensity))
        {
            UpdateExistingSegmentObstacles();
            m_LastKnownObstacleDensity = currentDensity;
        }
    }

    public void ChangeZone()
    {
        m_CurrentZone += 1;
        if (m_CurrentZone >= m_CurrentThemeData.zones.Length)
            m_CurrentZone = 0;

        m_CurrentZoneDistance = 0;
    }

    /// <summary>
    /// Determines whether obstacles should be spawned on a segment based on density settings
    /// </summary>
    /// <returns>True if obstacles should be spawned on this segment</returns>
    private bool ShouldSpawnObstaclesOnSegment()
    {
        float densityMultiplier = ITrackRunnerConfigProvider.Instance.ObstacleDensity;
        return Random.value <= densityMultiplier;
    }

    /// <summary>
    /// Updates existing segments to match current obstacle density
    /// </summary>
    private async void UpdateExistingSegmentObstacles()
    {
        // Update segments beyond the protection zone
        for (int i = protectedSegmentCount; i < segments.Count; i++)
        {
            await RecalculateSegmentObstacles(segments[i]);
        }
    }

    /// <summary>
    /// Recalculates obstacles for a specific segment based on current density
    /// </summary>
    /// <param name="segment">The segment to recalculate obstacles for</param>
    private async UniTask RecalculateSegmentObstacles(TrackSegment segment)
    {
        segment.CleanupObstacles();

        // Re-apply density-based spawning with physics checks
        if (ShouldSpawnObstaclesOnSegment())
        {
            await SpawnObstacle(segment);
        }
    }

    private async UniTask SpawnObstacle(TrackSegment segment)
    {
        if (segment.possibleObstacles.Length != 0)
        {
            for (int i = 0; i < segment.obstaclePositions.Length; ++i)
            {
                AssetReference assetRef = segment.possibleObstacles[Random.Range(0, segment.possibleObstacles.Length)];
                await SpawnFromAssetReference(assetRef, segment, i);
            }
        }
    }

    private async UniTask SpawnFromAssetReference(AssetReference reference, TrackSegment segment, int posIndex,
        CancellationToken cancellationToken = default)
    {
        // Get the spawn position for physics check
        Vector3 spawnPosition;
        Quaternion spawnRotation;
        segment.GetPointAt(segment.obstaclePositions[posIndex], out spawnPosition, out spawnRotation);

        // Physics check before spawning
        if (Physics.CheckSphere(spawnPosition, obstacleCheckRadius, obstacleCheckLayerMask))
        {
            // Position is occupied, skip spawning this obstacle
            return;
        }

        AsyncOperationHandle op = Addressables.LoadAssetAsync<GameObject>(reference);
        await op.WithCancellation(cancellationToken);
        GameObject obj = op.Result as GameObject;
        if (obj != null)
        {
            Obstacle obstacle = obj.GetComponent<Obstacle>();
            if (obstacle != null)
                await obstacle.Spawn(segment, segment.obstaclePositions[posIndex]);
        }
    }

    #region Virtual Origin System Helper Methods

    /// <summary>
    /// Gets the virtual world position for a given distance traveled
    /// </summary>
    private Vector3 GetVirtualWorldPosition(float distance)
    {
        return m_VirtualOrigin + Vector3.forward * (distance - m_VirtualOriginDistance);
    }

    /// <summary>
    /// Resets virtual origin when distance threshold is reached to maintain precision
    /// </summary>
    private void UpdateVirtualOrigin()
    {
        if (m_TotalWorldDistance > k_FloatingOriginThreshold)
        {
            if (enableSegmentDebugLogs)
                Debug.Log($"[TRACK_DEBUG] Updating virtual origin: TotalDistance={m_TotalWorldDistance:F2}, " +
                          $"PreviousOriginDistance={m_VirtualOriginDistance:F2}");

            m_VirtualOriginDistance += k_FloatingOriginThreshold;
            m_TotalWorldDistance -= k_FloatingOriginThreshold;
            m_VirtualOrigin = Vector3.zero; // Keep origin at zero, adjust distance tracking
        }
    }

    /// <summary>
    /// Checks if a segment should be cleaned up based on distance traveled
    /// </summary>
    private bool ShouldCleanupSegment(TrackSegment segment)
    {
        if (!m_SegmentPassDistances.TryGetValue(segment, out float passDistance))
            return false;

        float distanceSincePass = (m_TotalWorldDistance + m_VirtualOriginDistance) - passDistance;
        return distanceSincePass > Math.Abs(k_SegmentRemovalDistance);
    }

    #endregion

    public float GetMultiplier()
    {
        return 1 + Mathf.Floor((speed - ITrackRunnerConfigProvider.Instance.MinSpeed) /
                               (ITrackRunnerConfigProvider.Instance.MaxSpeed -
                                ITrackRunnerConfigProvider.Instance.MinSpeed) *
                               ITrackRunnerConfigProvider.Instance.SpeedStep);
    }
}