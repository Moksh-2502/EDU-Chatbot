using System.Collections.Generic;
using System.Linq;
using FluencySDK;

namespace ReusablePatterns.FluencySDK.Scripts.Runtime.LearningProgress.Models
{
    /// <summary>
    /// Represents the overall learning progress state of a fact set
    /// </summary>
    public class FactSetProgress
    {
        public FactSet FactSet { get; }
        private readonly IList<FactItem> _factItems;
        private readonly LearningAlgorithmConfig _config;

        public FactSetProgress(FactSet factSet, IList<FactItem> factItems, LearningAlgorithmConfig config)
        {
            FactSet = factSet;
            _factItems = factItems;
            _config = config;
        }

        public LearningStage GetDominantStage()
        {
            if (_factItems.Count == 0)
            {
                return null;
            }

            var stageGroups = _factItems.GroupBy(f => f.StageId);
            var leastAdvancedStage = stageGroups
                .OrderBy(g => _config.GetStageById(g.Key)?.Order ?? int.MaxValue)
                .First();

            return _config.GetStageById(leastAdvancedStage.Key);
        }

        public string GetDominantStageName()
        {
            return GetDominantStage()?.GetShortName() ?? "Unknown";
        }

        /// <summary>
        /// Calculates the overall progress percentage for this fact set
        /// Based on weighted progression through learning stages
        /// </summary>
        public float GetProgressPercentage()
        {
            if (_factItems.Count == 0)
                return 0f;

            float totalProgress = 0f;

            foreach (var factItem in _factItems)
            {
                var stage = _config.GetStageById(factItem.StageId);
                totalProgress += stage?.ProgressWeight ?? 0f;
            }

            return (totalProgress / _factItems.Count) * 100f;
        }

        /// <summary>
        /// Gets the total number of facts in this fact set
        /// </summary>
        public int GetTotalFactsCount()
        {
            return _factItems.Count;
        }

        /// <summary>
        /// Gets the number of facts that have been completed
        /// </summary>
        public int GetCompletedFactsCount()
        {
            return _factItems.Count(f => {
                var stage = _config.GetStageById(f.StageId);
                return stage != null && stage.IsFullyLearned;
            });
        }

        public bool CanClaimReward()
        {
            return _factItems.Count > 0 &&
                   _factItems.All(f => {
                       var stage = _config.GetStageById(f.StageId);
                       return stage != null && stage.IsRewardEligible;
                   });
        }

        public bool IsCompleted()
        {
            return _factItems.Count > 0 && _factItems.All(f => {
                var stage = _config.GetStageById(f.StageId);
                return stage != null && stage.IsFullyLearned;
            });
        }

        /// <summary>
        /// Gets the distribution of facts across learning stages
        /// </summary>
        public Dictionary<LearningStage, int> GetStageDistribution()
        {
            var distribution = new Dictionary<LearningStage, int>();

            // Initialize all stages with 0
            foreach (var stage in _config.Stages)
            {
                distribution[stage] = 0;
            }

            // Count facts in each stage
            foreach (var factItem in _factItems)
            {
                var stage = _config.GetStageById(factItem.StageId);
                if (distribution.ContainsKey(stage))
                {
                    distribution[stage]++;
                }
            }

            return distribution;
        }

        /// <summary>
        /// Gets read-only access to the fact items for detailed view
        /// </summary>
        public IReadOnlyList<FactItem> GetFactItems()
        {
            return _factItems.ToList().AsReadOnly();
        }

        /// <summary>
        /// Gets the number of facts in a specific learning stage
        /// </summary>
        public int GetFactsCountInStage(string stageId)
        {
            return _factItems.Count(f => f.StageId == stageId);
        }

        /// <summary>
        /// Gets the most advanced stage that has facts
        /// </summary>
        public LearningStage GetMostAdvancedStage()
        {
            if (_factItems.Count == 0)
                return _config.GetFirstStage();

            var mostAdvancedFactItem = _factItems
                .OrderByDescending(f => _config.GetStageById(f.StageId)?.Order ?? -1)
                .First();
            
            return _config.GetStageById(mostAdvancedFactItem.StageId);
        }

        /// <summary>
        /// Gets the least advanced stage that has facts (bottleneck)
        /// </summary>
        public LearningStage GetLeastAdvancedStage()
        {
            if (_factItems.Count == 0)
                return _config.GetFirstStage();

            var leastAdvancedFactItem = _factItems
                .OrderBy(f => _config.GetStageById(f.StageId)?.Order ?? int.MaxValue)
                .First();
            
            return _config.GetStageById(leastAdvancedFactItem.StageId);
        }

        /// <summary>
        /// Creates FactItemProgress objects combining facts with their statistics
        /// </summary>
        /// <param name="studentState">The current student state containing fact statistics</param>
        /// <returns>Collection of FactItemProgress objects for detailed view</returns>
        public IReadOnlyList<FactItemProgress> GetFactItemsWithStats(StudentState studentState)
        {
            var factItemProgresses = new List<FactItemProgress>();

            foreach (var factItem in _factItems)
            {
                // Get the stats for this fact, or use empty stats if not found
                var stats = studentState?.Stats?.ContainsKey(factItem.FactId) == true
                    ? studentState.Stats[factItem.FactId]
                    : new FactStats();

                factItemProgresses.Add(new FactItemProgress(factItem, stats, _config));
            }

            return factItemProgresses.AsReadOnly();
        }

        /// <summary>
        /// Gets facts that need attention (low accuracy or high error streaks)
        /// </summary>
        /// <param name="studentState">The current student state containing fact statistics</param>
        /// <returns>Collection of facts that need attention</returns>
        public IReadOnlyList<FactItemProgress> GetFactsNeedingAttention(StudentState studentState)
        {
            return GetFactItemsWithStats(studentState)
                .Where(fip => fip.NeedsAttention())
                .ToList()
                .AsReadOnly();
        }


    }
}