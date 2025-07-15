using System;
using System.Collections.Generic;
using UnityEngine;
using ReusablePatterns.FluencySDK.Scripts.Runtime.LearningProgress.Models;

namespace ReusablePatterns.FluencySDK.Scripts.Runtime.LearningProgress.UI
{
    /// <summary>
    /// Handles rendering of the navigation panel with fact set list and overview button
    /// </summary>
    public class LearningNavigationPanel
    {
        private readonly LearningHudStyleManager _styleManager;
        private readonly LearningHudProgressRenderer _progressRenderer;
        
        private Vector2 _scrollPosition = Vector2.zero;

        public event Action OnOverviewSelected;
        public event Action<FactSetProgress, int> OnFactSetSelected;

        public LearningNavigationPanel(LearningHudStyleManager styleManager, LearningHudProgressRenderer progressRenderer)
        {
            _styleManager = styleManager;
            _progressRenderer = progressRenderer;
        }

        public void Draw(float availableHeight, IList<FactSetProgress> factSetProgresses, 
                        bool overviewSelected, int selectedFactSetIndex)
        {
            // Begin scroll view for the entire panel
            _scrollPosition = GUILayout.BeginScrollView(_scrollPosition, GUILayout.Height(availableHeight));
            
            GUILayout.BeginVertical(_styleManager.PanelStyle);
            
            GUILayout.Label("🧭 NAVIGATION", _styleManager.HeaderStyle);
            
            // Overview button (always first)
            var overviewButtonStyle = overviewSelected ? _styleManager.SelectedButtonStyle : _styleManager.ButtonStyle;
            
            if (GUILayout.Button("📊 Overview\nOverall learning statistics", overviewButtonStyle, GUILayout.Height(55)))
            {
                OnOverviewSelected?.Invoke();
            }
            
            // Show overall progress bar if we have data
            if (factSetProgresses != null && factSetProgresses.Count > 0)
            {
                var overallStats = ILearningProgressService.Instance.CalculateOverallStatistics(factSetProgresses);
                _progressRenderer.DrawMiniProgressBar(overallStats.OverallProgressPercent / 100f, 
                    _styleManager.GetProgressColor(overallStats.OverallProgressPercent));
            }
            else
            {
                _progressRenderer.DrawMiniProgressBar(0f, _styleManager.ProgressUnknownColor);
            }
            
            GUILayout.Space(5);
            
            // Fact set buttons
            if (factSetProgresses != null && factSetProgresses.Count > 0)
            {
                for (int i = 0; i < factSetProgresses.Count; i++)
                {
                    var factSet = factSetProgresses[i];
                    var isSelected = selectedFactSetIndex == i && !overviewSelected;
                    
                    GUILayout.BeginHorizontal();
                    
                    // Fact set button
                    var buttonStyle = isSelected ? _styleManager.SelectedButtonStyle : _styleManager.ButtonStyle;
                    var progressIcon = _styleManager.GetProgressIcon(factSet.GetProgressPercentage());
                    var buttonText = $"{progressIcon} {factSet.FactSet.Id}\n{factSet.GetDominantStage()} • {factSet.GetProgressPercentage():F1}%";
                    
                    if (GUILayout.Button(buttonText, buttonStyle, GUILayout.Height(55)))
                    {
                        OnFactSetSelected?.Invoke(factSet, i);
                    }
                    
                    GUILayout.EndHorizontal();
                    
                    // Progress bar for this fact set
                    _progressRenderer.DrawMiniProgressBar(factSet.GetProgressPercentage() / 100f, 
                        _styleManager.GetProgressColor(factSet.GetProgressPercentage()));
                    
                    GUILayout.Space(2);
                }
            }
            else
            {
                GUILayout.Label("No fact sets available", _styleManager.LabelStyle);
            }
            
            // Add bottom padding to ensure last element is fully visible
            GUILayout.Space(20);
            
            GUILayout.EndVertical();
            GUILayout.EndScrollView();
        }

        public void ResetScrollPosition()
        {
            _scrollPosition = Vector2.zero;
        }
    }
} 