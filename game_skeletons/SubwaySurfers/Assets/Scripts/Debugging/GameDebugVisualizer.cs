using UnityEngine;
using UnityEngine.SceneManagement;
using System.Linq;
using Characters;
using FluencySDK;
using ReusablePatterns.SharedCore.Scripts.Runtime.Debugging;
using ReusablePatterns.FluencySDK.Scripts.Runtime.LearningProgress;
using SubwaySurfers.DifficultySystem;
using SubwaySurfers.Utilities;
using SharedCore;

namespace SubwaySurfers.Debugging
{
    /// <summary>
    /// Simple OnGUI visualizer for the difficulty system debug information
    /// Implemented as a singleton that persists across scene loads
    /// </summary>
    public class GameDebugVisualizer : MonoBehaviour
    {
        // Singleton implementation
        private static GameDebugVisualizer _instance;

        [Header("Visualization Settings")] [SerializeField]
        private bool showDebugPanel = true;

        [SerializeField] private KeyCode toggleKey = KeyCode.F1;
        [SerializeField] private Vector2 panelPosition = new Vector2(10, 10);
        [SerializeField] private Vector2 panelSize = new Vector2(350, 800);

        [Header("Tab Settings")] [SerializeField]
        private DebugTab activeTab = DebugTab.Track;

        private enum DebugTab
        {
            Track = 0,
            Character = 1,
            Education = 2,
            Difficulty = 3,
            Build = 4
        }

        [Header("Display Settings")] [SerializeField] [Range(5, 30)]
        private int contentPadding = 15;

        [SerializeField] private int fontSize = 19;
        [SerializeField] private float tabWidth = 85f;

        // References
        private IDifficultyProvider _difficultyProvider;
        private GameDifficultyController gameDifficultyController;
        private TrackManager trackManager;
        private CharacterInputController characterController;
        private Character character;
        private CharacterCollider characterCollider;
        private IQuestionProvider questionProvider;
        private GameBuildConfig buildConfig;
        private LootablesSpawner lootablesSpawner;

        // GUI Style
        private GUIStyle boxStyle;
        private GUIStyle labelStyle;
        private GUIStyle headerStyle;
        private GUIStyle activeTabStyle;
        private GUIStyle inactiveTabStyle;
        private bool stylesInitialized = false;

        // Style refresh tracking
        private int lastFontSize = -1;
        private int lastContentPadding = -1;
        private float lastTabWidth = -1f;

        // Scroll position
        private Vector2 scrollPosition = Vector2.zero;

        // Scene change tracking
        private bool refreshNeeded = false;

        private void Awake()
        {
            // Singleton pattern implementation
            if (_instance == null)
            {
                _instance = this;
                DontDestroyOnLoad(gameObject);

                // Check if debug mode should be enabled via URL parameter
                if (StringUtils.IsDebugModeEnabledViaUrl())
                {
                    showDebugPanel = true;
                }

                InitializeReferences();

                // Subscribe to scene change events
                SceneManager.sceneLoaded += OnSceneLoaded;
            }
            else if (_instance != this)
            {
                // Destroy duplicate instances
                Destroy(gameObject);
                return;
            }
        }

        /// <summary>
        /// Called when a new scene is loaded
        /// </summary>
        /// <param name="scene">The loaded scene</param>
        /// <param name="mode">The load mode</param>
        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            // Mark that references need to be refreshed
            refreshNeeded = true;

            // Force style refresh after scene change
            stylesInitialized = false;

            // Ensure the GameObject stays active
            if (gameObject != null)
            {
                gameObject.SetActive(true);
            }

            Debug.Log($"GameDebugVisualizer: Scene '{scene.name}' loaded, refreshing references and styles");
        }

        private void InitializeReferences()
        {
            // Find references
            _difficultyProvider = FindFirstObjectByType<DifficultyManager>(FindObjectsInactive.Include);
            gameDifficultyController = FindFirstObjectByType<GameDifficultyController>(FindObjectsInactive.Include);

            trackManager = FindFirstObjectByType<TrackManager>(FindObjectsInactive.Include);
            characterController = FindFirstObjectByType<CharacterInputController>(FindObjectsInactive.Include);
            characterCollider = FindFirstObjectByType<CharacterCollider>(FindObjectsInactive.Include);
            lootablesSpawner = FindFirstObjectByType<LootablesSpawner>(FindObjectsInactive.Include);

            // Find educational system components
            // The BaseQuestionProvider is a singleton, so we can access it directly
            questionProvider = FluencySDK.Unity.BaseQuestionProvider.Instance;

            // Load build configuration
            buildConfig = GameBuildConfigLoader.LoadBuildConfiguration();
        }

        private void Update()
        {
            // Toggle debug panel with key press
            if (Input.GetKeyDown(toggleKey))
            {
                showDebugPanel = !showDebugPanel;
            }

            // Refresh references if scene changed
            if (refreshNeeded)
            {
                RefreshReferencesIfNeeded();
            }

            // Update character reference if needed
            if (trackManager != null && trackManager.characterController != null &&
                characterController != trackManager.characterController)
            {
                characterController = trackManager.characterController;
                characterCollider = characterController.characterCollider;
                character = characterController.character;
            }
        }

        /// <summary>
        /// Refreshes object references after scene changes
        /// </summary>
        private void RefreshReferencesIfNeeded()
        {
            // Refresh all references
            InitializeReferences();

            // Reset the refresh flag
            refreshNeeded = false;
        }

        /// <summary>
        /// Shows the debug panel
        /// </summary>
        public void ShowDebugPanel()
        {
            showDebugPanel = true;
        }

        /// <summary>
        /// Hides the debug panel
        /// </summary>
        public void HideDebugPanel()
        {
            showDebugPanel = false;
        }

        /// <summary>
        /// Toggles the debug panel visibility
        /// </summary>
        public void ToggleDebugPanel()
        {
            showDebugPanel = !showDebugPanel;
        }

        /// <summary>
        /// Gets whether the debug panel is currently visible
        /// </summary>
        public bool IsDebugPanelVisible => showDebugPanel;

        /// <summary>
        /// Forces a complete refresh of references and styles (useful for debugging)
        /// </summary>
        public void ForceRefresh()
        {
            Debug.Log("GameDebugVisualizer: Force refresh requested");
            refreshNeeded = true;
            stylesInitialized = false;

            // Ensure GameObject is active
            if (gameObject != null)
            {
                gameObject.SetActive(true);
            }
        }

        private void OnGUI()
        {
            if (!showDebugPanel) return;

            // Ensure GameObject is active (shouldn't be necessary but just in case)
            if (!gameObject.activeInHierarchy)
            {
                Debug.LogWarning("GameDebugVisualizer: GameObject is not active during OnGUI");
                return;
            }

            InitializeStyles();
            DrawDebugPanel();
        }

        private void InitializeStyles()
        {
            // Check if we need to refresh styles due to value changes
            bool needsRefresh = !stylesInitialized ||
                                lastFontSize != fontSize ||
                                lastContentPadding != contentPadding ||
                                !Mathf.Approximately(lastTabWidth, tabWidth);

            if (!needsRefresh) return;

            // Clean up existing textures before creating new ones
            CleanupStyleTextures();

            Debug.Log("GameDebugVisualizer: Initializing GUI styles");

            boxStyle = new GUIStyle(GUI.skin.box)
            {
                normal = { background = MakeColorTexture(new Color(0, 0, 0, 0.95f)) },
                padding = new RectOffset(contentPadding, contentPadding, contentPadding, contentPadding)
            };

            headerStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = fontSize + 2,
                fontStyle = FontStyle.Bold,
                normal = { textColor = Color.yellow }
            };

            labelStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = fontSize,
                normal = { textColor = Color.white },
                wordWrap = true,
                margin = new RectOffset(5, 0, 2, 2)
            };

            // Tab styles
            activeTabStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = fontSize - 2,
                fontStyle = FontStyle.Bold,
                normal = { textColor = Color.yellow, background = MakeColorTexture(new Color(0.4f, 0.4f, 0.4f, 0.8f)) },
                hover = { textColor = Color.yellow, background = MakeColorTexture(new Color(0.5f, 0.5f, 0.5f, 0.8f)) },
                active = { textColor = Color.yellow, background = MakeColorTexture(new Color(0.4f, 0.4f, 0.4f, 0.8f)) }
            };

            inactiveTabStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = fontSize - 2,
                normal = { textColor = Color.white, background = MakeColorTexture(new Color(0.2f, 0.2f, 0.2f, 0.8f)) },
                hover = { textColor = Color.white, background = MakeColorTexture(new Color(0.3f, 0.3f, 0.3f, 0.8f)) },
                active =
                {
                    textColor = Color.white, background = MakeColorTexture(new Color(0.25f, 0.25f, 0.25f, 0.8f))
                }
            };

            // Update tracking values
            lastFontSize = fontSize;
            lastContentPadding = contentPadding;
            lastTabWidth = tabWidth;
            stylesInitialized = true;
        }

        private void DrawDebugPanel()
        {
            Rect panelRect = new Rect(panelPosition.x, panelPosition.y, panelSize.x, panelSize.y);
            GUI.Box(panelRect, "", boxStyle);

            GUILayout.BeginArea(panelRect);
            GUILayout.BeginVertical();

            // Header (fixed at top)
            GUILayout.Label($"Press {toggleKey} to toggle", labelStyle);
            GUILayout.Space(5);

            // Panel size controls
            DrawPanelSizeControls();
            GUILayout.Space(5);

            // Tab navigation (fixed at top)
            DrawTabNavigation();
            GUILayout.Space(5);

            // Scrollable content area
            scrollPosition = GUILayout.BeginScrollView(scrollPosition);

            // Active tab content
            switch (activeTab)
            {
                case DebugTab.Track:
                    DrawTrackManagerInfo();
                    if (trackManager != null)
                    {
                        GUILayout.Space(15);
                        DrawTrackControls();
                    }

                    break;

                case DebugTab.Character:
                    DrawCharacterInfo();
                    if (characterController != null)
                    {
                        GUILayout.Space(15);
                        DrawCharacterControls();
                    }

                    break;

                case DebugTab.Education:
                    DrawEducationInfo();
                    break;

                case DebugTab.Difficulty:
                    DrawDifficultyInfo();
                    GUILayout.Space(12);
                    DrawSystemState();
                    GUILayout.Space(12);
                    DrawAdjustersInfo();
                    GUILayout.Space(12);
                    DrawDifficultyControls();
                    break;

                case DebugTab.Build:
                    DrawBuildInfo();
                    break;
            }

            var debugInfoProviders = IDebugInfoRegistry.Instance.GetProviders();

            foreach (var provider in debugInfoProviders)
            {
                try
                {
                    if (provider == null || provider.DebugGroupName != activeTab.ToString())
                    {
                        continue;
                    }

                    GUILayout.Space(15);
                    GUILayout.Label(provider.DebugGroupName, headerStyle);
                    GUILayout.Label(provider.GetDebugInfo(), labelStyle);
                }
                catch (System.Exception ex)
                {
                    GUILayout.Label($"Error drawing debug info: {ex.Message}", labelStyle);
                }
            }

            GUILayout.EndScrollView();
            GUILayout.EndVertical();
            GUILayout.EndArea();
        }

        private void DrawPanelSizeControls()
        {
            GUILayout.Label("Panel Size", labelStyle);

            GUILayout.BeginHorizontal();
            GUILayout.Label("Width:", GUILayout.Width(50));
            panelSize.x = GUILayout.HorizontalSlider(panelSize.x, 250f, Screen.width, GUILayout.Width(150));
            GUILayout.Label($"{panelSize.x:F0}", GUILayout.Width(40));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Height:", GUILayout.Width(50));
            panelSize.y = GUILayout.HorizontalSlider(panelSize.y, 400f, Screen.height, GUILayout.Width(150));
            GUILayout.Label($"{panelSize.y:F0}", GUILayout.Width(40));
            GUILayout.EndHorizontal();
        }

        private void DrawTabNavigation()
        {
            string[] tabNames = { "Track", "Character", "Education", "Difficulty", "Build" };

            // Calculate how many tabs can fit per row based on panel width
            int tabsPerRow = Mathf.Max(1, Mathf.FloorToInt((panelSize.x - (contentPadding * 2)) / tabWidth));

            // Use SelectionGrid for automatic wrapping
            int selectedTab = GUILayout.SelectionGrid(
                (int)activeTab,
                tabNames,
                tabsPerRow,
                activeTabStyle,
                GUILayout.Width(panelSize.x - (contentPadding * 2))
            );

            // Update active tab if selection changed
            if (selectedTab != (int)activeTab)
            {
                activeTab = (DebugTab)selectedTab;
            }
        }

        private void DrawTrackManagerInfo()
        {
            GUILayout.Label("TRACK MANAGER", headerStyle);

            if (trackManager != null)
            {
                // Speed and movement info
                GUILayout.Label($"Speed: {trackManager.speed:F2} (Ratio: {trackManager.speedRatio:F2})", labelStyle);
                GUILayout.Label($"Moving: {trackManager.isMoving}", labelStyle);
                GUILayout.Label($"Rerun: {trackManager.isRerun} | Loaded: {trackManager.isLoaded}", labelStyle);

                // Distance tracking
                GUILayout.Label($"World Distance: {trackManager.worldDistance:F1}m", labelStyle);
                GUILayout.Label($"Segment Distance: {trackManager.currentSegmentDistance:F1}m", labelStyle);

                // Zone and theme info
                GUILayout.Label($"Current Zone: {trackManager.currentZone}", labelStyle);
                if (trackManager.currentTheme != null)
                {
                    GUILayout.Label($"Theme: {trackManager.currentTheme.themeName}", labelStyle);
                }

                // Additional technical info
                GUILayout.Label(
                    $"Track Seed: {(trackManager.trackSeed == -1 ? "Random" : trackManager.trackSeed.ToString())}",
                    labelStyle);
            }
            else
            {
                GUILayout.Label("TrackManager not found", labelStyle);
            }
        }

        private void DrawCharacterInfo()
        {
            GUILayout.Label("CHARACTER SYSTEM", headerStyle);

            // Player State Information (consolidated)
            if (IPlayerStateProvider.Instance != null)
            {
                GUILayout.Label("PLAYER STATE", headerStyle);
                GUILayout.Label(
                    $"Lives: {IPlayerStateProvider.Instance.CurrentLives}/{IPlayerStateProvider.Instance.MaxLives}",
                    labelStyle);
                GUILayout.Label($"Total Lives Lost: {IPlayerStateProvider.Instance.TotalLostLives}", labelStyle);
                GUILayout.Label($"Can Gain Lives: {IPlayerStateProvider.Instance.CanGainLives()}", labelStyle);
                GUILayout.Label($"Is Alive: {IPlayerStateProvider.Instance.IsAlive}", labelStyle);
                GUILayout.Label($"Score: {IPlayerStateProvider.Instance.RunScore:n0}", labelStyle);
                GUILayout.Label($"Coins This Run: {IPlayerStateProvider.Instance.RunCoins:n0}", labelStyle);
                GUILayout.Label(
                    $"Total score multipliers sum: {IPlayerStateProvider.Instance.GetTotalMultiplier():F2}");
                GUILayout.Space(10);
            }

            if (characterController != null)
            {
                // Basic character info
                if (character != null)
                {
                    GUILayout.Label("CHARACTER INFO", headerStyle);
                    GUILayout.Label($"Character: {character.characterName}", labelStyle);
                    GUILayout.Label($"Cost: {character.cost} / Premium: {character.premiumCost}", labelStyle);
                }

                // Movement states
                GUILayout.Label(
                    $"Lane: {GetCurrentLane()} | Jumping: {characterController.isJumping} | Sliding: {characterController.isSliding}",
                    labelStyle);

                // Consumables
                if (characterController.consumables.Count > 0)
                {
                    string consumableNames = string.Join(", ",
                        characterController.consumables.ConvertAll(c => c.GetType().Name));
                    GUILayout.Label($"Active Consumables: {consumableNames}", labelStyle);
                }
                else
                {
                    GUILayout.Label("Active Consumables: None", labelStyle);
                }

                // Inventory
                if (characterController.inventory != null)
                {
                    GUILayout.Label($"Inventory: {characterController.inventory.GetType().Name}", labelStyle);
                }
                else
                {
                    GUILayout.Label("Inventory: Empty", labelStyle);
                }
            }
            else
            {
                GUILayout.Label("CharacterController not found", labelStyle);
            }

            // Character collider info
            if (characterCollider != null)
            {
                GUILayout.Label($"Invincible: {!characterCollider.CanTakeLife()}", labelStyle);
                GUILayout.Label($"Shielded: {characterCollider.IsShielded()}", labelStyle);
                GUILayout.Label($"Magnet Coins: {characterCollider.magnetCoins.Count}", labelStyle);
            }
            else
            {
                GUILayout.Label("CharacterCollider not found", labelStyle);
            }
        }

        private string GetCurrentLane()
        {
            if (characterController == null) return "Unknown";

            // Calculate current lane based on target position
            // Lane 0 = Left, Lane 1 = Center, Lane 2 = Right
            var targetPos = characterController.characterCollider.transform.localPosition;
            float laneOffset = trackManager != null ? trackManager.laneOffset : 1.0f;

            int lane = Mathf.RoundToInt(targetPos.x / laneOffset) + 1;
            lane = Mathf.Clamp(lane, 0, 2);

            string[] laneNames = { "Left", "Center", "Right" };
            return $"{lane} ({laneNames[lane]})";
        }

        private void DrawEducationInfo()
        {
            GUILayout.Label("LEARNING SYSTEM", headerStyle);

            if (questionProvider != null)
            {
                // Student state details
                if (questionProvider.StudentState != null)
                {
                    var currentQuestionError = GetQuestionProviderError();
                    if (!string.IsNullOrEmpty(currentQuestionError))
                    {
                        GUILayout.Label($"Status: {currentQuestionError}", labelStyle);
                    }
                    else
                    {
                        GUILayout.Label("Status: Ready", labelStyle);
                    }

                    var studentState = questionProvider.StudentState;

                    DrawStageOverview(studentState);
                    GUILayout.Space(10);

                    DrawFactSetDetails(studentState);
                    GUILayout.Space(10);

                    DrawEducationControls(studentState);
                    GUILayout.Space(10);

                    DrawRecentActivity(studentState);
                    GUILayout.Space(10);

                    DrawOverallStats(studentState);
                }
                else
                {
                    GUILayout.Label("Student State: Not initialized", labelStyle);
                }
            }
            else
            {
                GUILayout.Label("Question Provider: Not found", labelStyle);
            }
        }


        private void DrawStageOverview(StudentState studentState)
        {
            GUILayout.Label("STAGE DISTRIBUTION", headerStyle);

            if (ILearningProgressService.Instance == null)
            {
                GUILayout.Label("Learning Progress Service not available", labelStyle);
                return;
            }

            // Get overall statistics from the learning progress service
            var overallStats = ILearningProgressService.Instance.CalculateOverallStatistics();
            var stageDistribution = overallStats.StageDistribution;

            var totalFacts = stageDistribution.Values.Sum();
            if (totalFacts == 0)
            {
                GUILayout.Label("No facts available", labelStyle);
                return;
            }

            // Show stages in progression order
            foreach (var stage in questionProvider.Config.Stages)
            {
                var count = stageDistribution.ContainsKey(stage) ? stageDistribution[stage] : 0;
                var percentage = (float)count / totalFacts * 100;
                var icon = stage.Icon;

                GUILayout.Label($"{icon} {stage}: {count} facts ({percentage:F1}%)", labelStyle);
            }

            // Show overall progress summary
            GUILayout.Space(5);
            GUILayout.Label($"Total Facts: {totalFacts}", labelStyle);
            GUILayout.Label($"Overall Progress: {overallStats.OverallProgressPercent:F1}%", labelStyle);

            if (overallStats.OverallAccuracy > 0)
            {
                GUILayout.Label($"Overall Accuracy: {overallStats.OverallAccuracy:P1}", labelStyle);
            }
        }

        private void DrawFactSetDetails(StudentState studentState)
        {
            GUILayout.Label("FACT SET STATUS", headerStyle);

            if (ILearningProgressService.Instance == null)
            {
                GUILayout.Label("Learning Progress Service not available", labelStyle);
                return;
            }

            var factSetProgresses = ILearningProgressService.Instance.GetFactSetProgresses();

            if (factSetProgresses.Count == 0)
            {
                GUILayout.Label("No fact sets available", labelStyle);
                return;
            }

            // Filter active fact sets (not completed)
            var activeFactSets = factSetProgresses.Where(fs => fs.IsCompleted() == false).ToList();
            var completedFactSets = factSetProgresses.Count(fs => fs.IsCompleted());

            GUILayout.Label($"Active Fact Sets: {activeFactSets.Count}", labelStyle);
            GUILayout.Label($"Completed Fact Sets: {completedFactSets}", labelStyle);

            foreach (var factSetProgress in activeFactSets.Take(8)) // Limit to avoid UI overflow
            {
                var factSetId = factSetProgress.FactSet.Id;
                var totalFacts = factSetProgress.GetTotalFactsCount();
                var progressPercent = factSetProgress.GetProgressPercentage();
                var dominantStage = factSetProgress.GetDominantStage();

                GUILayout.Label($"   {factSetId}: {totalFacts} facts ({progressPercent:F1}%, {dominantStage})",
                    labelStyle);
            }

            if (activeFactSets.Count > 8)
            {
                GUILayout.Label($"   ... and {activeFactSets.Count - 8} more", labelStyle);
            }

            // Recent answers for current fact set  
            if (studentState.AnswerHistory != null && studentState.AnswerHistory.Count > 0)
            {
                var totalAnswers = studentState.AnswerHistory.Count;
                GUILayout.Label($"Total Answers Given: {totalAnswers}", labelStyle);
            }
        }

        private void DrawRecentActivity(StudentState studentState)
        {
            GUILayout.Label("RECENT ACTIVITY", headerStyle);

            if (studentState.AnswerHistory != null && studentState.AnswerHistory.Count > 0)
            {
                var recentAnswers = studentState.AnswerHistory.TakeLast(5).ToList();

                GUILayout.Label($"Last {recentAnswers.Count} answers:", labelStyle);
                foreach (var answer in recentAnswers)
                {
                    var result = answer.AnswerType == AnswerType.Correct ? "✓" : "✗";
                    var stage = questionProvider.Config.GetStageById(answer.StageId).DisplayName;
                    var time = answer.AnswerTime.ToString("HH:mm:ss");
                    GUILayout.Label($"   {result} {answer.FactId} [{stage}] at {time}", labelStyle);
                }
            }
            else
            {
                GUILayout.Label("No recent activity", labelStyle);
            }
        }

        private string CalculateStageAccuracy(System.Collections.Generic.List<AnswerRecord> answers,
            LearningStage stage)
        {
            var stageAnswers = answers.Where(a => a.StageId == stage.Id).ToList();
            if (stageAnswers.Count == 0) return "N/A";

            var correct = stageAnswers.Count(a => a.AnswerType == AnswerType.Correct);
            var accuracy = (float)correct / stageAnswers.Count;
            return $"{accuracy:P0}";
        }

        private void DrawEducationControls(StudentState studentState)
        {
            GUILayout.Label("EDUCATION CONTROLS", headerStyle);

            if (studentState == null || questionProvider?.Algorithm?.FactSets == null)
            {
                GUILayout.Label("No fact sets available", labelStyle);
                return;
            }

            // Fact set selection (only for LearningAlgorithmV3)
            if (questionProvider.Algorithm is FluencySDK.LearningAlgorithmV3 v3)
            {
                GUILayout.Label("Set Fact Set To Review:", labelStyle);
                foreach (var item in v3.FactSets.Values)
                {
                    if (GUILayout.Button($"{item.Id}"))
                    {
                        SetFactsToReviewExceptOneV3(studentState, item, v3.PromotionEngine);
                    }
                }
            }
        }

        private void SetFactsToReviewExceptOneV3(StudentState studentState, FactSet factSet, FluencySDK.Algorithm.PromotionEngine promotionEngine)
        {
            if (questionProvider?.StudentState == null)
            {
                Debug.LogError("Student state not available");
                return;
            }

            var facts = studentState.GetFactsForSet(factSet.Id);

            if (facts.Count == 0)
            {
                Debug.LogWarning($"No facts found for fact set: {factSet.Id}");
                return;
            }

            int promotedCount = 0;

            foreach (var fact in facts)
            {
                var firstReviewStage = questionProvider.Config.Stages
                    .OrderBy(s => s.Order)
                    .FirstOrDefault(s => s.Type == LearningStageType.Review);
                while (questionProvider.Config.GetStageById(fact.StageId).Order < firstReviewStage.Order)
                {
                    promotionEngine.PromoteFact(fact);
                    promotedCount++;
                }
            }
        }

        private void DrawOverallStats(StudentState studentState)
        {
            GUILayout.Label("OVERALL STATISTICS", headerStyle);

            if (ILearningProgressService.Instance == null)
            {
                GUILayout.Label("Learning Progress Service not available", labelStyle);
                return;
            }

            var overallStats = ILearningProgressService.Instance.CalculateOverallStatistics();

            // Basic counts and progress
            GUILayout.Label(
                $"Total Questions: {overallStats.TotalAttempts} ({overallStats.TotalAttempts - (overallStats.TotalAttempts - (int)(overallStats.TotalAttempts * overallStats.OverallAccuracy))} correct)",
                labelStyle);
            GUILayout.Label($"Overall Accuracy: {overallStats.OverallAccuracy:P1}", labelStyle);
            GUILayout.Label($"Current Streak: {overallStats.CurrentStreak}", labelStyle);

            GUILayout.Space(5);
            GUILayout.Label(
                $"Total Fact Sets: {overallStats.TotalFactSets} ({overallStats.CompletedFactSets} completed)",
                labelStyle);
            GUILayout.Label($"Total Facts: {overallStats.TotalFacts} ({overallStats.CompletedFacts} completed)",
                labelStyle);
            GUILayout.Label($"Facts Needing Attention: {overallStats.StrugglingFactsCount}", labelStyle);
            GUILayout.Label($"Mastered Facts: {overallStats.MasteredFactsCount}", labelStyle);

            // Legacy detailed stats from StudentState (if available)
            if (studentState?.Stats != null && studentState.Stats.Count > 0)
            {
                GUILayout.Space(5);
                GUILayout.Label($"Unique Facts Seen: {studentState.Stats.Count}", labelStyle);

                // Find most/least accurate facts
                var factStats = studentState.Stats.Where(kvp => kvp.Value.TimesShown >= 3);
                if (factStats.Any())
                {
                    var bestFact = factStats
                        .OrderByDescending(kvp => (float)kvp.Value.TimesCorrect / kvp.Value.TimesShown).First();
                    var worstFact = factStats.OrderBy(kvp => (float)kvp.Value.TimesCorrect / kvp.Value.TimesShown)
                        .First();

                    var bestAccuracy = (float)bestFact.Value.TimesCorrect / bestFact.Value.TimesShown;
                    var worstAccuracy = (float)worstFact.Value.TimesCorrect / worstFact.Value.TimesShown;

                    GUILayout.Label($"Best fact: {bestFact.Key} ({bestAccuracy:P0})", labelStyle);
                    GUILayout.Label($"Struggling fact: {worstFact.Key} ({worstAccuracy:P0})", labelStyle);
                }
            }
        }

        private string GetQuestionProviderError()
        {
            if (questionProvider == null) return "Provider not found";

            try
            {
                // Use reflection to access the private GetCanShowQuestionError method
                return questionProvider.GetCanShowQuestionError();
            }
            catch (System.Exception)
            {
                // Fallback if reflection fails
            }

            return "Status unknown";
        }

        private void DrawDifficultyInfo()
        {
            GUILayout.Label("CURRENT DIFFICULTY", headerStyle);

            if (_difficultyProvider != null)
            {
                var currentConfig = _difficultyProvider.CurrentDifficultyConfig;

                GUILayout.Label(
                    $"Level: {_difficultyProvider.CurrentDifficultyLevelIndex} ({currentConfig?.displayName ?? "Unknown"})",
                    labelStyle);

                if (currentConfig != null)
                {
                    GUILayout.Label($"Speed Range: {currentConfig.speedRange}", labelStyle);
                    GUILayout.Label($"Acceleration: {currentConfig.accelerationRate:F3}", labelStyle);
                    GUILayout.Label($"Obstacle Density: {currentConfig.obstacleDensityMultiplier:F2}x", labelStyle);

                    if (currentConfig.collectableConfig != null)
                    {
                        var collectables = currentConfig.collectableConfig;
                        GUILayout.Label($"Collectables: Base {collectables.baseFrequency:F2}x", labelStyle);
                    }
                }
            }
            else
            {
                GUILayout.Label("DifficultyManager not found", labelStyle);
            }
        }

        private void DrawSystemState()
        {
            GUILayout.Label("SYSTEM STATE", headerStyle);

            if (_difficultyProvider != null)
            {
                GUILayout.Label($"Adjustment Enabled: {_difficultyProvider.IsDifficultyAdjustmentEnabled}", labelStyle);
                GUILayout.Label($"Time-Based Mode: {_difficultyProvider.IsTimeBasedMode}", labelStyle);

                if (_difficultyProvider.CanChangeDifficulty())
                {
                    GUILayout.Label("Can Change: Yes", labelStyle);
                }
                else
                {
                    float cooldownRemaining = _difficultyProvider.GetCooldownTimeRemaining();
                    GUILayout.Label($"Cooldown: {cooldownRemaining:F1}s", labelStyle);
                }
            }

            if (gameDifficultyController != null)
            {
                GUILayout.Label("Game Controller: Active", labelStyle);
            }
            else
            {
                GUILayout.Label("Game Controller: Missing", labelStyle);
            }
        }

        private void DrawAdjustersInfo()
        {
            GUILayout.Label("ADJUSTERS", headerStyle);

            // Life-based adjuster
            if (_difficultyProvider.LifeBasedAdjuster != null)
            {
                GUILayout.Label($"Life-Based: Active", labelStyle);
                // Note: We can't access private fields, but we can show it's present
            }
            else
            {
                GUILayout.Label("Life-Based: Missing", labelStyle);
            }

            // Time-based adjuster
            if (_difficultyProvider.TimeBasedAdjuster != null)
            {
                GUILayout.Label($"Time-Based: Active", labelStyle);
                // Note: We can't access private fields, but we can show it's present
            }
            else
            {
                GUILayout.Label("Time-Based: Missing", labelStyle);
            }
        }

        private void DrawTrackControls()
        {
            GUILayout.Label("TRACK CONTROLS", headerStyle);

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Start Move", GUILayout.Width(80)))
            {
                trackManager.StartMove(false); // Don't reset speed
            }

            if (GUILayout.Button("Stop Move", GUILayout.Width(80)))
            {
                trackManager.StopMove();
            }

            if (GUILayout.Button("Change Zone", GUILayout.Width(90)))
            {
                trackManager.ChangeZone();
            }

            GUILayout.EndHorizontal();

            // Powerup spawn controls
            if (lootablesSpawner != null)
            {
                GUILayout.Space(10);
                GUILayout.Label("POWERUP SPAWN CONTROLS", headerStyle);

                // Powerup multiplier
                GUILayout.BeginHorizontal();
                GUILayout.Label($"Powerup ({lootablesSpawner.PowerupSpawnMultiplier})", GUILayout.Width(60));
                lootablesSpawner.PowerupSpawnMultiplier =
                    GUILayout.HorizontalSlider(lootablesSpawner.PowerupSpawnMultiplier, 0, 1, GUILayout.Width(100));
                GUILayout.EndHorizontal();

                // Premium multiplier
                GUILayout.BeginHorizontal();
                GUILayout.Label($"Premium ({lootablesSpawner.PremiumSpawnMultiplier})", GUILayout.Width(60));
                lootablesSpawner.PremiumSpawnMultiplier =
                    GUILayout.HorizontalSlider(lootablesSpawner.PremiumSpawnMultiplier, 0, 1, GUILayout.Width(100));
                GUILayout.EndHorizontal();
            }
            else
            {
                GUILayout.Space(10);
                GUILayout.Label("POWERUP SPAWN CONTROLS", headerStyle);
                GUILayout.Label("LootablesSpawner not found", labelStyle);
            }
        }

        private void DrawCharacterControls()
        {
            GUILayout.Label("CHARACTER CONTROLS", headerStyle);

            // Life controls
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Add Life", GUILayout.Width(70)))
            {
                IPlayerStateProvider.Instance.ChangeLives(1);
            }

            if (GUILayout.Button("Remove Life", GUILayout.Width(85)))
            {
                IPlayerStateProvider.Instance.ChangeLives(-1);
            }

            if (GUILayout.Button("Add Coins", GUILayout.Width(80)))
            {
                IPlayerStateProvider.Instance.ProcessPickedCoins(100);
            }

            GUILayout.EndHorizontal();

            // Movement controls
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Jump", GUILayout.Width(50)))
            {
                characterController.Jump();
            }

            if (GUILayout.Button("Slide", GUILayout.Width(50)))
            {
                characterController.Slide();
            }

            if (GUILayout.Button("Stop Moving", GUILayout.Width(90)))
            {
                characterController.StopMoving();
            }

            GUILayout.EndHorizontal();

            // State controls
            if (characterCollider != null)
            {
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("Toggle Invincible", GUILayout.Width(110)))
                {
                    if (characterCollider.CanTakeLife())
                        characterCollider.SetInvincible(10f);
                    else
                        characterCollider.SetInvincibleExplicit(false);
                }

                if (GUILayout.Button("Clear Consumables", GUILayout.Width(120)))
                {
                    characterController.CleanConsumable();
                }

                GUILayout.EndHorizontal();
            }
        }

        private void DrawDifficultyControls()
        {
            GUILayout.Label("DIFFICULTY CONTROLS", headerStyle);

            if (_difficultyProvider == null)
            {
                GUILayout.Label("No DifficultyManager available", labelStyle);
                return;
            }

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Reset", GUILayout.Width(60)))
            {
                _difficultyProvider.ResetDifficulty();
            }

            if (GUILayout.Button("↓", GUILayout.Width(30)))
            {
                _difficultyProvider.DecreaseDifficulty(ignoreRules: true);
            }

            if (GUILayout.Button("↑", GUILayout.Width(30)))
            {
                _difficultyProvider.IncreaseDifficulty(ignoreRules: true);
            }

            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Toggle Adjustment", GUILayout.Width(120)))
            {
                _difficultyProvider.SetDifficultyAdjustmentEnabled(!_difficultyProvider.IsDifficultyAdjustmentEnabled);
            }

            if (GUILayout.Button("Toggle Mode", GUILayout.Width(90)))
            {
                _difficultyProvider.SetTimeBasedMode(!_difficultyProvider.IsTimeBasedMode);
            }

            GUILayout.EndHorizontal();
        }

        private void DrawBuildInfo()
        {
            GUILayout.Label("BUILD INFORMATION", headerStyle);

            if (buildConfig != null)
            {
                GUILayout.Label($"Version: {buildConfig.BuildVersion}", labelStyle);
                GUILayout.Label($"Environment: {buildConfig.Environment}", labelStyle);
                GUILayout.Label($"Build ID: {buildConfig.BuildId}", labelStyle);
                GUILayout.Label($"Build Date: {buildConfig.BuildDate}", labelStyle);
                GUILayout.Label($"Build Version: {buildConfig.BuildVersion}", labelStyle);

                GUILayout.Space(10);
                GUILayout.Label("ENVIRONMENT INFO", headerStyle);

                // Show environment-specific info
                switch (buildConfig.Environment)
                {
                    case GameBuildConfig.EditorEnvironment:
                        GUILayout.Label("🔧 Editor Environment", labelStyle);
                        GUILayout.Label("Development mode active", labelStyle);
                        break;
                    case GameBuildConfig.DevelopmentEnvironment:
                        GUILayout.Label("🚧 Development Build", labelStyle);
                        GUILayout.Label("Debug features enabled", labelStyle);
                        break;
                    case GameBuildConfig.StagingEnvironment:
                        GUILayout.Label("🧪 Staging Build", labelStyle);
                        GUILayout.Label("Testing environment", labelStyle);
                        break;
                    case GameBuildConfig.ProductionEnvironment:
                        GUILayout.Label("🚀 Production Build", labelStyle);
                        GUILayout.Label("Release version", labelStyle);
                        break;
                    default:
                        GUILayout.Label($"📦 {buildConfig.Environment}", labelStyle);
                        break;
                }

                GUILayout.Space(10);
                GUILayout.Label("UNITY INFO", headerStyle);
                GUILayout.Label($"Unity Version: {Application.unityVersion}", labelStyle);
                GUILayout.Label($"Platform: {Application.platform}", labelStyle);
                GUILayout.Label($"System Language: {Application.systemLanguage}", labelStyle);
                GUILayout.Label($"Target Frame Rate: {Application.targetFrameRate}", labelStyle);

#if UNITY_EDITOR
                GUILayout.Label("Running in Unity Editor", labelStyle);
#elif DEVELOPMENT_BUILD
                GUILayout.Label("Development Build", labelStyle);
#else
                GUILayout.Label("Release Build", labelStyle);
#endif

                GUILayout.Space(10);
                GUILayout.Label("SYSTEM INFO", headerStyle);
                GUILayout.Label($"Device Model: {SystemInfo.deviceModel}", labelStyle);
                GUILayout.Label($"Operating System: {SystemInfo.operatingSystem}", labelStyle);
                GUILayout.Label($"Memory Size: {SystemInfo.systemMemorySize} MB", labelStyle);
                GUILayout.Label($"Graphics Device: {SystemInfo.graphicsDeviceName}", labelStyle);
                GUILayout.Label($"Graphics Memory: {SystemInfo.graphicsMemorySize} MB", labelStyle);
                GUILayout.Label($"Screen Resolution: {Screen.width}x{Screen.height}", labelStyle);
                GUILayout.Label($"Screen DPI: {Screen.dpi:F1}", labelStyle);
            }
            else
            {
                GUILayout.Label("Build configuration not found", labelStyle);
                GUILayout.Space(10);

                if (GUILayout.Button("Reload Build Config", GUILayout.Width(150)))
                {
                    buildConfig = GameBuildConfigLoader.LoadBuildConfiguration();
                }
            }
        }

        private Texture2D MakeColorTexture(Color color)
        {
            Texture2D texture = new Texture2D(1, 1);
            texture.SetPixel(0, 0, color);
            texture.Apply();
            return texture;
        }

        private void CleanupStyleTextures()
        {
            // Clean up existing textures to prevent memory leaks
            if (boxStyle?.normal?.background != null)
            {
                DestroyImmediate(boxStyle.normal.background);
            }

            if (activeTabStyle?.normal?.background != null)
            {
                DestroyImmediate(activeTabStyle.normal.background);
            }

            if (activeTabStyle?.hover?.background != null)
            {
                DestroyImmediate(activeTabStyle.hover.background);
            }

            if (activeTabStyle?.active?.background != null)
            {
                DestroyImmediate(activeTabStyle.active.background);
            }

            if (inactiveTabStyle?.normal?.background != null)
            {
                DestroyImmediate(inactiveTabStyle.normal.background);
            }

            if (inactiveTabStyle?.hover?.background != null)
            {
                DestroyImmediate(inactiveTabStyle.hover.background);
            }

            if (inactiveTabStyle?.active?.background != null)
            {
                DestroyImmediate(inactiveTabStyle.active.background);
            }
        }

        private void OnDestroy()
        {
            // Unsubscribe from scene change events
            SceneManager.sceneLoaded -= OnSceneLoaded;

            CleanupStyleTextures();

            // Clear singleton reference if this is the instance being destroyed
            if (_instance == this)
            {
                _instance = null;
            }
        }
    }
}