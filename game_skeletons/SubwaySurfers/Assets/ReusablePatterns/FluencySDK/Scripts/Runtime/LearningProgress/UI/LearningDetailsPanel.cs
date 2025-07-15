using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using FluencySDK;
using ReusablePatterns.FluencySDK.Scripts.Runtime.LearningProgress.Models;

namespace ReusablePatterns.FluencySDK.Scripts.Runtime.LearningProgress.UI
{
    /// <summary>
    /// Handles rendering of the details panel with both overall and fact set specific views
    /// </summary>
    public class LearningDetailsPanel
    {
        private readonly LearningHudStyleManager _styleManager;
        private readonly LearningHudProgressRenderer _progressRenderer;
        private readonly IQuestionProvider _questionProvider;

        private float _detailsPanelWidth = 500f;
        private Vector2 _detailsScrollPosition = Vector2.zero;

        public LearningDetailsPanel(LearningHudStyleManager styleManager,
                                   LearningHudProgressRenderer progressRenderer,
                                   IQuestionProvider questionProvider)
        {
            _styleManager = styleManager;
            _progressRenderer = progressRenderer;
            _questionProvider = questionProvider;
        }

        public void DrawOverallDetails(float availableHeight, IList<FactSetProgress> factSetProgresses)
        {
            var tempRect = GUILayoutUtility.GetRect(0, 0, GUILayout.ExpandWidth(true));
            if (tempRect.width > 20f)
            {
                _detailsPanelWidth = tempRect.width - 20f;
            }

            if (factSetProgresses == null || factSetProgresses.Count == 0)
            {
                GUILayout.BeginVertical(_styleManager.PanelStyle, GUILayout.Height(availableHeight));
                GUILayout.Label("📈 DETAILED ANALYTICS", _styleManager.HeaderStyle);
                GUILayout.Label("No data available", _styleManager.LabelStyle);
                GUILayout.EndVertical();
                return;
            }

            // Begin scroll view for the entire panel
            _detailsScrollPosition = GUILayout.BeginScrollView(_detailsScrollPosition, GUILayout.Height(availableHeight));

            GUILayout.BeginVertical(_styleManager.PanelStyle);

            GUILayout.Label("📈 DETAILED ANALYTICS", _styleManager.HeaderStyle);

            var overallStats = ILearningProgressService.Instance.CalculateOverallStatistics(factSetProgresses);

            // Overall Statistics
            GUILayout.Label("📊 Overall Statistics", _styleManager.SubHeaderStyle);
            GUILayout.Label($"Total Fact Sets: {overallStats.TotalFactSets}", _styleManager.LabelStyle);
            GUILayout.Label($"Completed Fact Sets: {overallStats.CompletedFactSets}", _styleManager.LabelStyle);
            GUILayout.Label($"Total Facts: {overallStats.TotalFacts}", _styleManager.LabelStyle);
            GUILayout.Label($"Completed Facts: {overallStats.CompletedFacts}", _styleManager.LabelStyle);
            GUILayout.Label($"Overall Progress: {overallStats.OverallProgressPercent:F1}%", _styleManager.LabelStyle);
            _progressRenderer.DrawProgressBar(overallStats.OverallProgressPercent / 100f,
                _styleManager.GetProgressColor(overallStats.OverallProgressPercent));

            GUILayout.Space(10);

            // Stage Distribution
            GUILayout.Label("🎯 Stage Distribution", _styleManager.SubHeaderStyle);
            var totalFacts = overallStats.TotalFacts;

            foreach (var kvp in overallStats.StageDistribution.OrderBy(x => x.Key.Order))
            {
                var stage = kvp.Key;
                var count = kvp.Value;
                var percentage = totalFacts > 0 ? (float)count / totalFacts * 100f : 0f;

                var icon = stage.Icon;
                GUILayout.Label($"{icon} {stage}: {count} facts ({percentage:F1}%)", _styleManager.LabelStyle);
                _progressRenderer.DrawProgressBar(percentage / 100f, _styleManager.GetStageColor(stage));
                GUILayout.Space(2);
            }

            GUILayout.Space(10);

            // Performance Insights
            GUILayout.Label("🏆 Performance Insights", _styleManager.SubHeaderStyle);
            GUILayout.Label($"Current Streak: {overallStats.CurrentStreak}", _styleManager.LabelStyle);
            GUILayout.Label($"Total Attempts: {overallStats.TotalAttempts}", _styleManager.LabelStyle);
            GUILayout.Label($"Overall Accuracy: {overallStats.OverallAccuracy:P1}", _styleManager.LabelStyle);
            GUILayout.Label($"Facts Needing Attention: {overallStats.StrugglingFactsCount}", _styleManager.LabelStyle);
            GUILayout.Label($"Mastered Facts: {overallStats.MasteredFactsCount}", _styleManager.LabelStyle);

            // Add bottom padding to ensure last element is fully visible
            GUILayout.Space(20);

            GUILayout.EndVertical();
            GUILayout.EndScrollView();
        }

        public void DrawFactSetDetails(float availableHeight, FactSetProgress selectedFactSet)
        {
            if (selectedFactSet == null)
            {
                GUILayout.BeginVertical(_styleManager.PanelStyle, GUILayout.Height(availableHeight));
                GUILayout.Label("📚 FACT SET: No Selection", _styleManager.HeaderStyle);
                GUILayout.Label("No fact set selected", _styleManager.LabelStyle);
                GUILayout.EndVertical();
                return;
            }

            // Begin scroll view for the entire panel
            _detailsScrollPosition = GUILayout.BeginScrollView(_detailsScrollPosition, GUILayout.Height(availableHeight));

            GUILayout.BeginVertical(_styleManager.PanelStyle);

            GUILayout.Label($"📚 FACT SET: {selectedFactSet.FactSet.Id}", _styleManager.HeaderStyle);

            // Fact Set Overview
            GUILayout.Label("📋 Overview", _styleManager.SubHeaderStyle);
            GUILayout.Label($"Dominant Stage: {selectedFactSet.GetDominantStageName()}", _styleManager.LabelStyle);
            GUILayout.Label($"Progress: {selectedFactSet.GetProgressPercentage():F1}%", _styleManager.LabelStyle);
            _progressRenderer.DrawProgressBar(selectedFactSet.GetProgressPercentage() / 100f,
                _styleManager.GetProgressColor(selectedFactSet.GetProgressPercentage()));

            GUILayout.Label($"Total Facts: {selectedFactSet.GetTotalFactsCount()}", _styleManager.LabelStyle);
            GUILayout.Label($"Completed Facts: {selectedFactSet.GetCompletedFactsCount()}", _styleManager.LabelStyle);
            GUILayout.Label($"Can Claim: {(selectedFactSet.CanClaimReward() ? "Yes" : "No")}", _styleManager.LabelStyle);

            GUILayout.Space(10);

            // Stage Distribution for this fact set
            GUILayout.Label("🎯 Stage Distribution", _styleManager.SubHeaderStyle);
            var distribution = selectedFactSet.GetStageDistribution();
            var totalFactsInSet = selectedFactSet.GetTotalFactsCount();

            foreach (var kvp in distribution.OrderBy(x => x.Key.Order))
            {
                var stage = kvp.Key;
                var count = kvp.Value;
                var percentage = totalFactsInSet > 0 ? (float)count / totalFactsInSet * 100f : 0f;

                var icon = stage.Icon;
                GUILayout.Label($"{icon} {stage}: {count} facts ({percentage:F1}%)", _styleManager.LabelStyle);
                _progressRenderer.DrawProgressBar(percentage / 100f, _styleManager.GetStageColor(stage));
                GUILayout.Space(2);
            }

            GUILayout.Space(10);

            // Individual Facts (if we have StudentState)
            var studentState = _questionProvider?.StudentState;
            if (studentState != null)
            {
                DrawIndividualFacts(selectedFactSet, studentState);
            }
            else
            {
                GUILayout.Label("Student state not available for detailed fact information", _styleManager.LabelStyle);
            }

            // Add bottom padding to ensure last element is fully visible
            GUILayout.Space(20);

            GUILayout.EndVertical();
            GUILayout.EndScrollView();
        }

        private void DrawIndividualFacts(FactSetProgress selectedFactSet, StudentState studentState)
        {
            GUILayout.Label("🔍 Individual Facts", _styleManager.SubHeaderStyle);

            var factItems = selectedFactSet.GetFactItems();

            // Calculate basic statistics
            var totalFacts = factItems.Count;
            var factsWithStats = factItems.Where(f => studentState.Stats?.ContainsKey(f.FactId) == true).ToList();
            var totalAttempts = factsWithStats.Sum(f => studentState.Stats[f.FactId].TimesShown);
            var totalCorrect = factsWithStats.Sum(f => studentState.Stats[f.FactId].TimesCorrect);
            var averageAccuracy = totalAttempts > 0 ? (float)totalCorrect / totalAttempts : 0f;

            // Count struggling facts (accuracy < 60% and attempts >= 3)
            var strugglingFacts = factsWithStats.Where(f =>
            {
                var stats = studentState.Stats[f.FactId];
                var accuracy = stats.TimesShown > 0 ? (float)stats.TimesCorrect / stats.TimesShown : 0f;
                return stats.TimesShown >= 3 && accuracy < 0.6f;
            }).ToList();

            GUILayout.Label($"Total Facts: {totalFacts}", _styleManager.LabelStyle);
            GUILayout.Label($"Facts with Data: {factsWithStats.Count}", _styleManager.LabelStyle);
            GUILayout.Label($"Facts Needing Attention: {strugglingFacts.Count}", _styleManager.LabelStyle);
            GUILayout.Label($"Average Accuracy: {averageAccuracy:P1}", _styleManager.LabelStyle);

            GUILayout.Space(5);

            // Sort facts: struggling first, then by stage progression order, then by ID
            var sortedFacts = factItems
                .OrderBy(f =>
                {
                    if (studentState.Stats?.ContainsKey(f.FactId) == true)
                    {
                        var stats = studentState.Stats[f.FactId];
                        var accuracy = stats.TimesShown > 0 ? (float)stats.TimesCorrect / stats.TimesShown : 0f;
                        return stats.TimesShown >= 3 && accuracy < 0.6f ? 0 : 1; // struggling facts first
                    }
                    return 2; // facts without data last
                })
                .ThenBy(f => _questionProvider.Config.GetStageById(f.StageId)?.Order ?? int.MaxValue)
                .ThenBy(f => f.FactId)
                .ToList();

            DrawFactsTable(sortedFacts, studentState);
        }

        private void DrawFactsTable(List<FactItem> factItems, StudentState studentState)
        {
            var availableWidth = Mathf.Max(500f, _detailsPanelWidth);

            var factIdWidth = (int)(availableWidth * 0.10f);
            var stageWidth = (int)(availableWidth * 0.20f);
            var accuracyWidth = (int)(availableWidth * 0.20f);
            var attemptsWidth = (int)(availableWidth * 0.20f);
            var statusWidth = (int)(availableWidth * 0.30f);

            GUILayout.BeginHorizontal();
            GUILayout.Label("Fact", _styleManager.TableHeaderStyle, GUILayout.Width(factIdWidth));
            GUILayout.Label("Stage", _styleManager.TableHeaderStyle, GUILayout.Width(stageWidth));
            GUILayout.Label("Accuracy", _styleManager.TableHeaderStyle, GUILayout.Width(accuracyWidth));
            GUILayout.Label("Attempts", _styleManager.TableHeaderStyle, GUILayout.Width(attemptsWidth));
            GUILayout.Label("Status", _styleManager.TableHeaderStyle, GUILayout.Width(statusWidth));
            GUILayout.EndHorizontal();

            GUILayout.Space(2);
            var separatorRect = GUILayoutUtility.GetRect(availableWidth, 1);
            var originalColor = GUI.color;
            GUI.color = Color.gray;
            GUI.DrawTexture(separatorRect, Texture2D.whiteTexture);
            GUI.color = originalColor;
            GUILayout.Space(2);

            foreach (var factItem in factItems)
            {
                var hasStats = studentState.Stats?.ContainsKey(factItem.FactId) == true;
                var stage = _questionProvider.Config.GetStageById(factItem.StageId);

                string accuracyText = "-";
                string attemptsText = "0";
                string statusText = "Not Attempted";

                if (hasStats)
                {
                    var stats = studentState.Stats[factItem.FactId];
                    var accuracy = stats.TimesShown > 0 ? (float)stats.TimesCorrect / stats.TimesShown : 0f;
                    var needsAttention = stats.TimesShown >= 3 && accuracy < 0.6f;

                    accuracyText = $"{accuracy:P1}";
                    attemptsText = stats.TimesShown.ToString();
                    statusText = "Learning";

                    if (stage.IsFullyLearned)
                    {
                        statusText = "Mastered";
                    }
                    else if (needsAttention)
                    {
                        statusText = "Needs Practice";
                    }
                    else if (accuracy >= 0.8f)
                    {
                        statusText = "Doing Well";
                    }
                    else if (accuracy >= 0.6f)
                    {
                        statusText = "Good Progress";
                    }
                }

                GUILayout.BeginHorizontal();
                GUILayout.Label(factItem.FactId, _styleManager.TableCellStyle, GUILayout.Width(factIdWidth));
                GUILayout.Label(stage.DisplayName, _styleManager.TableCellStyle, GUILayout.Width(stageWidth));
                GUILayout.Label(accuracyText, _styleManager.TableCellStyle, GUILayout.Width(accuracyWidth));
                GUILayout.Label(attemptsText, _styleManager.TableCellStyle, GUILayout.Width(attemptsWidth));
                GUILayout.Label(statusText, _styleManager.TableCellStyle, GUILayout.Width(statusWidth));
                GUILayout.EndHorizontal();

                GUILayout.Space(1);
            }
        }

        public void ResetScrollPositions()
        {
            _detailsScrollPosition = Vector2.zero;
        }
    }
}