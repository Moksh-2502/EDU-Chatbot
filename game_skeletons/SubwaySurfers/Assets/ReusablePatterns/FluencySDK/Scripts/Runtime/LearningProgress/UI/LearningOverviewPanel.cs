using System.Collections.Generic;
using UnityEngine;
using FluencySDK;
using ReusablePatterns.FluencySDK.Scripts.Runtime.LearningProgress.Models;

namespace ReusablePatterns.FluencySDK.Scripts.Runtime.LearningProgress.UI
{
    /// <summary>
    /// Handles rendering of the overall learning progress overview panel
    /// </summary>
    public class LearningOverviewPanel
    {
        private readonly LearningHudStyleManager _styleManager;
        private readonly LearningHudProgressRenderer _progressRenderer;

        public LearningOverviewPanel(LearningHudStyleManager styleManager, LearningHudProgressRenderer progressRenderer)
        {
            _styleManager = styleManager;
            _progressRenderer = progressRenderer;
        }

        public void Draw(float panelHeight, IList<FactSetProgress> factSetProgresses)
        {
            GUILayout.BeginVertical(_styleManager.PanelStyle, GUILayout.Height(panelHeight));
            
            GUILayout.Label("📊 OVERALL PROGRESS", _styleManager.HeaderStyle);
            
            if (factSetProgresses != null && factSetProgresses.Count > 0)
            {
                var overallStats = ILearningProgressService.Instance.CalculateOverallStatistics(factSetProgresses);
                
                // Progress percentage with bar
                GUILayout.Label($"Overall Progress: {overallStats.OverallProgressPercent:F1}%", _styleManager.LabelStyle);
                _progressRenderer.DrawProgressBar(overallStats.OverallProgressPercent / 100f, 
                    _styleManager.GetProgressColor(overallStats.OverallProgressPercent));
                
                GUILayout.Space(5);
                
                // Summary stats
                GUILayout.Label($"Fact Sets: {overallStats.CompletedFactSets}/{overallStats.TotalFactSets}", _styleManager.LabelStyle);
                GUILayout.Label($"Facts: {overallStats.CompletedFacts}/{overallStats.TotalFacts}", _styleManager.LabelStyle);
                GUILayout.Label($"Current Streak: {overallStats.CurrentStreak}", _styleManager.LabelStyle);
            }
            else
            {
                GUILayout.Label("No learning data available", _styleManager.LabelStyle);
            }
            
            GUILayout.EndVertical();
        }
    }
} 