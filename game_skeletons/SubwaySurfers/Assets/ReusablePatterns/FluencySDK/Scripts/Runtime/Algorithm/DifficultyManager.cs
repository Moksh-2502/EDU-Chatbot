using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace FluencySDK.Algorithm
{
    public class DifficultyManager
    {
        private string _currentDifficulty;
        private readonly DynamicDifficultyConfig _config;

        public string CurrentDifficulty => _currentDifficulty;

        public DifficultyManager(DynamicDifficultyConfig config)
        {
            _config = config ?? throw new System.ArgumentNullException(nameof(config));

            if (_config.Difficulties.Count == 0)
            {
                throw new System.ArgumentException("No difficulties found in config");
            }
            else
            {
                _currentDifficulty = _config.Difficulties
                    .OrderBy(d => d.MinAccuracyThreshold)
                    .First().Name;
            }

            Debug.Log($"[DifficultyManager] Initialized with difficulty: {_currentDifficulty}");
        }

        public void UpdateDifficulty(List<AnswerRecord> recentAnswers)
        {
            if (recentAnswers == null)
            {
                return;
            }

            if (recentAnswers.Count < _config.MinAnswersForDifficultyChange)
            {
                return;
            }

            var correctAnswers = recentAnswers.Count(a => a.AnswerType == AnswerType.Correct);
            var accuracy = (float)correctAnswers / recentAnswers.Count;

            var selectedDifficulty = _config.Difficulties
                .Where(d => accuracy >= d.MinAccuracyThreshold)
                .OrderByDescending(d => d.MinAccuracyThreshold)
                .FirstOrDefault();

            if (selectedDifficulty != null && selectedDifficulty.Name != _currentDifficulty)
            {
                var oldDifficulty = _currentDifficulty;
                _currentDifficulty = selectedDifficulty.Name;
                Debug.Log($"[DifficultyManager] Difficulty changed: {oldDifficulty} -> {_currentDifficulty}");
            }
        }

        public DifficultyConfig GetCurrentDifficultyConfig()
        {
            var config = _config.Difficulties.FirstOrDefault(d => d.Name == _currentDifficulty) ??
                         _config.Difficulties.FirstOrDefault();
            return config;
        }
    }
}