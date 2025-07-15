using System.Collections.Generic;
using UnityEngine;
using FluencySDK;
using FluencySDK.Unity;
using ReusablePatterns.FluencySDK.Scripts.Runtime.LearningProgress.Models;

namespace ReusablePatterns.FluencySDK.Scripts.Runtime.LearningProgress.UI
{
    /// <summary>
    /// OnGUI-based Learning HUD for parents and teachers
    /// Displays hierarchical learning progress with 3-panel layout
    /// Coordinates between different UI panel components
    /// </summary>
    public class LearningHudGUI : MonoBehaviour
    {
        [Header("Display Settings")]
        [SerializeField] private bool showHUD = false;
        [SerializeField] private KeyCode toggleKey = KeyCode.F2;
        [SerializeField] private float padding = 20f;

        [Header("Layout Settings")]
        [SerializeField] private float leftPanelWidth = 350f;
        [SerializeField] private float overviewPanelHeight = 200f;
        [SerializeField] private float panelSpacing = 10f;

        [Header("Visual Settings")]
        [SerializeField] private float contentPadding = 15f;
        [SerializeField] private int fontSize = 14;
        [SerializeField] private int headerFontSize = 16;
        [SerializeField] private float cornerRadius = 8f;
        [SerializeField] private float shadowOffset = 2f;
        [SerializeField] private float animationSpeed = 0.3f;
        [SerializeField] private bool enableAnimations = true;

        // Data and state
        private IQuestionProvider _questionProvider;
        private IList<FactSetProgress> _currentFactSetProgresses;
        private bool _initialized = false;

        // UI State
        private bool _showingOverview = true;
        private FactSetProgress _selectedFactSet = null;
        private int _selectedFactSetIndex = -1;

        // UI Components
        private LearningHudStyleManager _styleManager;
        private LearningHudProgressRenderer _progressRenderer;
        private LearningOverviewPanel _overviewPanel;
        private LearningNavigationPanel _navigationPanel;
        private LearningDetailsPanel _detailsPanel;

        private void Awake()
        {
            Initialize();
        }

        private void Initialize()
        {
            if (_initialized) return;

            _initialized = true;
            _questionProvider = BaseQuestionProvider.Instance;
            
            // Initialize UI components
            InitializeUIComponents();
        }

        private void InitializeUIComponents()
        {
            // Create style manager and configure it
            _styleManager = new LearningHudStyleManager
            {
                ContentPadding = contentPadding,
                FontSize = fontSize,
                HeaderFontSize = headerFontSize,
                CornerRadius = cornerRadius,
                ShadowOffset = shadowOffset
            };

            // Create progress renderer
            _progressRenderer = new LearningHudProgressRenderer(_styleManager);

            // Create panel components
            _overviewPanel = new LearningOverviewPanel(_styleManager, _progressRenderer);
            _navigationPanel = new LearningNavigationPanel(_styleManager, _progressRenderer);
            _detailsPanel = new LearningDetailsPanel(_styleManager, _progressRenderer, _questionProvider);

            // Subscribe to navigation events
            _navigationPanel.OnOverviewSelected += ShowOverallDetails;
            _navigationPanel.OnFactSetSelected += ShowFactSetDetails;
        }

        private void Update()
        {
            if (Input.GetKeyDown(toggleKey))
            {
                ToggleHUD();
            }
        }

        private void OnGUI()
        {
            if (!showHUD) return;

            InitializeStylesIfNeeded();
            RefreshDataIfNeeded();
            DrawLearningHUD();
        }

        private void InitializeStylesIfNeeded()
        {
            _styleManager?.Initialize();
        }

        // Event handlers for navigation
        private void ShowOverallDetails()
        {
            _showingOverview = true;
            _selectedFactSet = null;
            _selectedFactSetIndex = -1;
            _detailsPanel.ResetScrollPositions();
        }

        private void ShowFactSetDetails(FactSetProgress factSetProgress, int index)
        {
            _showingOverview = false;
            _selectedFactSet = factSetProgress;
            _selectedFactSetIndex = index;
            _detailsPanel.ResetScrollPositions();
        }

        private void RefreshDataIfNeeded()
        {
            _currentFactSetProgresses = ILearningProgressService.Instance.GetFactSetProgresses();
        }

        private void DrawLearningHUD()
        {
            float screenWidth = Screen.width;
            float screenHeight = Screen.height;
            
            // Calculate panel dimensions
            float hudWidth = screenWidth - (padding * 2);
            float hudHeight = screenHeight - (padding * 2);

            // Main HUD background
            Rect hudRect = new Rect(padding, padding, hudWidth, hudHeight);
            GUI.Box(hudRect, "", _styleManager.BoxStyle);

            // Begin main area with content padding
            var contentRect = new Rect(hudRect.x + contentPadding, hudRect.y + contentPadding, 
                                     hudRect.width - (contentPadding * 2), hudRect.height - (contentPadding * 2));
            GUILayout.BeginArea(contentRect);
            GUILayout.BeginVertical();

            // Header
            DrawHeader();

            // Calculate available height for content after header
            float headerHeight = headerFontSize + 20; // Approximate height of header + space
            float availableContentHeight = contentRect.height - headerHeight;

            // Three-panel layout
            GUILayout.BeginHorizontal(GUILayout.Height(availableContentHeight));

            // Left column (Overview + Navigation)
            DrawLeftColumn(availableContentHeight);

            GUILayout.Space(panelSpacing);

            // Right panel: Details
            DrawRightColumn(availableContentHeight);

            GUILayout.EndHorizontal();
            GUILayout.EndVertical();
            GUILayout.EndArea();
        }

        private void DrawHeader()
        {
            GUILayout.Label($"🎓 Learning Progress Dashboard", _styleManager.HeaderStyle);
            var headerSubStyle = new GUIStyle(_styleManager.LabelStyle) 
            { 
                fontSize = fontSize - 1, 
                normal = { textColor = _styleManager.TextSecondaryColor },
                fontStyle = FontStyle.Italic
            };
            GUILayout.Label($"Press {toggleKey} to toggle • Real-time learning analytics", headerSubStyle);
            GUILayout.Space(8);
        }

        private void DrawLeftColumn(float availableContentHeight)
        {
            GUILayout.BeginVertical(GUILayout.Width(leftPanelWidth - contentPadding));
            
            // Top-left: Overview Panel
            _overviewPanel.Draw(overviewPanelHeight, _currentFactSetProgresses);
            
            GUILayout.Space(panelSpacing);
            
            // Bottom-left: Navigation Panel
            float remainingLeftHeight = availableContentHeight - overviewPanelHeight - panelSpacing;
            _navigationPanel.Draw(remainingLeftHeight, _currentFactSetProgresses, _showingOverview, _selectedFactSetIndex);
            
            GUILayout.EndVertical();
        }

        private void DrawRightColumn(float availableContentHeight)
        {
            float rightPanelWidth = Screen.width - (padding * 2) - leftPanelWidth - panelSpacing;
            float availableRightWidth = rightPanelWidth - contentPadding;
            
            GUILayout.BeginVertical(GUILayout.Width(availableRightWidth));
            
            if (_showingOverview)
            {
                _detailsPanel.DrawOverallDetails(availableContentHeight, _currentFactSetProgresses);
            }
            else
            {
                _detailsPanel.DrawFactSetDetails(availableContentHeight, _selectedFactSet);
            }
            
            GUILayout.EndVertical();
        }

        // Public API
        public void ShowHUD()
        {
            showHUD = true;
        }

        public void HideHUD()
        {
            showHUD = false;
        }

        public void ToggleHUD()
        {
            showHUD = !showHUD;
        }

        public bool IsHUDVisible => showHUD;

        private void OnDestroy()
        {
            // Unsubscribe from events
            if (_navigationPanel != null)
            {
                _navigationPanel.OnOverviewSelected -= ShowOverallDetails;
                _navigationPanel.OnFactSetSelected -= ShowFactSetDetails;
            }
        }
    }
} 