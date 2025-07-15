using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FluencySDK
{
    public class SimpleFluencyGenerator : IFluencyGenerator
    {
        private StudentState _studentState;
        private FluencyGeneratorConfig _config;
        private readonly IStorageAdapter _storageAdapter;
        private readonly string _storageKey;
        private Dictionary<string, Question> _currentQuestionBlock = new Dictionary<string, Question>();
        private Random _random = new Random();

        public SimpleFluencyGenerator(IStorageAdapter storageAdapter, string storageKey = "fluencyState_default", FluencyGeneratorConfig config = null)
        {
            _storageAdapter = storageAdapter ?? throw new ArgumentNullException(nameof(storageAdapter));
            _storageKey = string.IsNullOrWhiteSpace(storageKey) ? "fluencyState_default" : storageKey;
            _config = config ?? new FluencyGeneratorConfig();
        }

        public async Task Initialize(FluencyGeneratorConfig config = null)
        {
            if (config != null)
            {
                _config = config;
            }

            _studentState = await _storageAdapter.GetItemAsync<StudentState>(_storageKey);
            if (_studentState == null)
            {
                _studentState = new StudentState();
                // Ensure sequence is populated if student state is new and config sequence is default
                // or if the config provided on initialize is different.
                _studentState.Mode = LearningMode.Placement; // Default starting mode
                _studentState.CurrentPosition = 0;
                await SaveStateAsync();
            }
        }

        private async Task SaveStateAsync()
        {
            await _storageAdapter.SetItemAsync(_storageKey, _studentState);
        }

        public Task<StudentState> GetStudentState()
        {
            if (_studentState == null)
            {
                // This case should ideally be handled by Initialize ensuring state is always loaded or created.
                // However, as a safeguard:
                throw new InvalidOperationException("Student state is not initialized. Call Initialize first.");
            }
            // Return a copy to prevent external modification of the internal state object
            var stateJson = JsonConvert.SerializeObject(_studentState);
            return Task.FromResult(JsonConvert.DeserializeObject<StudentState>(stateJson));
        }

        public async Task SetMode(LearningMode mode)
        {
            if (_studentState == null) throw new InvalidOperationException("Initialize first.");
            _studentState.Mode = mode;
            // Potentially reset current position or other logic depending on mode change rules
            // For now, just setting the mode.
            await SaveStateAsync();
        }

        public async Task ResetState()
        {
            _studentState = new StudentState();
            await _storageAdapter.RemoveItemAsync(_storageKey);
            await Initialize(_config);
        }

        public async Task<SubmitAnswerResult> SubmitAnswer(string questionId, int userAnswer, int responseTimeMs)
        {
            if (_studentState == null) throw new InvalidOperationException("Initialize first.");
            if (!_currentQuestionBlock.TryGetValue(questionId, out Question question))
            {
                throw new ArgumentException("Invalid question ID or question not in current block.", nameof(questionId));
            }

            question.UserAnswer = userAnswer;
            question.IsCorrect = question.Answer == userAnswer;
            question.TimeEnded = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(); // Assuming TimeStarted was set when block generated

            string factKey = GetFactKey(question.Factors);
            UpdateFactRecord(factKey, question.IsCorrect.Value, responseTimeMs);

            await SaveStateAsync();

            return new SubmitAnswerResult
            {
                IsCorrect = question.IsCorrect.Value,
                CorrectAnswer = question.IsCorrect.Value ? (int?)null : question.Answer
            };
        }

        private void UpdateFactRecord(string factKey, bool isCorrect, int responseTimeMs)
        {
            if (!_studentState.LearnedFacts.TryGetValue(factKey, out FactRecord record))
            {
                record = new FactRecord();
                _studentState.LearnedFacts[factKey] = record;
            }

            record.LastSeen = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            if (isCorrect)
            {
                record.TimesCorrect++;
            }
            else
            {
                record.TimesIncorrect++;
            }

            // Update average response time
            int totalResponses = record.TimesCorrect + record.TimesIncorrect;
            if (totalResponses == 1) // First response for this fact
            {
                record.AverageResponseTime = responseTimeMs;
            }
            else
            {
                // Weighted average: (oldAvg * (n-1) + newTime) / n
                record.AverageResponseTime = ((record.AverageResponseTime * (totalResponses - 1)) + responseTimeMs) / totalResponses;
            }
        }

        private string GetFactKey(int[] factors)
        {
            Array.Sort(factors);
            return string.Join("x", factors);
        }

        public async Task<IQuestion[]> GetNextQuestionBlock()
        {
            if (_studentState == null) throw new InvalidOperationException("Initialize first.");

            List<Question> questions = new List<Question>();
            _currentQuestionBlock.Clear(); // Clear previous block

            switch (_studentState.Mode)
            {
                case LearningMode.Placement:
                    questions.AddRange(GeneratePlacementQuestions());
                    break;
                case LearningMode.Learning:
                    questions.AddRange(GenerateLearningQuestions());
                    break;
                case LearningMode.Reinforcement:
                    questions.AddRange(GenerateReinforcementQuestions());
                    break;
                default:
                    throw new InvalidOperationException("Unknown learning mode.");
            }
            
            // Store for submission tracking and set TimeStarted
            long currentTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            foreach (var q in questions)
            {
                q.TimeStarted = currentTime;
                _currentQuestionBlock[q.Id] = q;
            }

            // If in learning mode and we successfully generated questions, advance position (simplistic for now)
            if (_studentState.Mode == LearningMode.Learning && questions.Any()){
                 // Basic advancement: if a block is generated, move to next item in sequence
                 // More sophisticated advancement would depend on performance on the current item.
                _studentState.CurrentPosition = (_studentState.CurrentPosition + 1) % _config.Sequence.Length;
            }

            await SaveStateAsync();
            return questions.ToArray();
        }

        private IEnumerable<Question> GeneratePlacementQuestions()
        {
            // Simple placement: select one random question for each number in the sequence up to QuestionsPerBlock
            // More sophisticated: ensure coverage of easy/medium/hard or specific ranges.
            List<Question> placementQuestions = new List<Question>();
            List<int> factorsToTest = new List<int>(_config.Sequence);
            
            // Shuffle factors to test to get varied blocks if called multiple times
            factorsToTest = factorsToTest.OrderBy(x => _random.Next()).ToList(); 

            for (int i = 0; i < Math.Min(_config.QuestionsPerBlock, factorsToTest.Count); i++)
            {
                int factor1 = factorsToTest[i];
                int factor2 = _random.Next(0, _config.MaxFactor + 1); // Multiplicand from 0 to MaxFactor
                placementQuestions.Add(CreateQuestion(factor1, factor2));
            }
            return placementQuestions;
        }

        private IEnumerable<Question> GenerateLearningQuestions()
        {
            // Focus on current sequence item, plus some review of prior items or incorrectly answered items.
            List<Question> learningQuestions = new List<Question>();
            int targetNewQuestions = _config.QuestionsPerBlock / 2; // Mix of new and review
            int currentSequenceValue = _config.Sequence[_studentState.CurrentPosition];

            // 1. Add new facts from the current sequence position
            for (int i = 0; i < targetNewQuestions; i++)
            {
                int factor2 = _random.Next(0, _config.MaxFactor + 1);
                learningQuestions.Add(CreateQuestion(currentSequenceValue, factor2));
            }

            // 2. Add review questions (facts answered incorrectly or due for review)
            var reviewCandidates = _studentState.LearnedFacts
                .Where(kvp => kvp.Value.TimesIncorrect > 0 || IsDueForReview(kvp.Value))
                .OrderBy(kvp => kvp.Value.LastSeen) // Prioritize least recently seen
                .Take(_config.QuestionsPerBlock - learningQuestions.Count)
                .ToList();

            foreach (var candidate in reviewCandidates)
            {
                var factors = ParseFactKey(candidate.Key);
                learningQuestions.Add(CreateQuestion(factors[0], factors[1]));
            }
            
            // Fill remaining spots with random earlier facts if needed
            int remainingSpots = _config.QuestionsPerBlock - learningQuestions.Count;
            if(remainingSpots > 0 && _studentState.CurrentPosition > 0)
            {
                for(int i = 0; i < remainingSpots; i++)
                {
                    int prevSequenceIndex = _random.Next(0, _studentState.CurrentPosition);
                    int factor1 = _config.Sequence[prevSequenceIndex];
                    int factor2 = _random.Next(0, _config.MaxFactor + 1);
                    learningQuestions.Add(CreateQuestion(factor1, factor2));
                }
            }
            // Ensure we don't exceed QuestionsPerBlock and shuffle for variety
            return learningQuestions.Take(_config.QuestionsPerBlock).OrderBy(q => _random.Next()).ToList();
        }

        private IEnumerable<Question> GenerateReinforcementQuestions()
        {
            // Focus on facts that are learned but need strengthening, or are due for spaced repetition.
            List<Question> reinforcementQuestions = new List<Question>();
            var candidates = _studentState.LearnedFacts
                .Where(kvp => kvp.Value.TimesCorrect > 0) // Only consider facts seen at least once correctly
                .OrderBy(kvp => kvp.Value.LastSeen)      // Prioritize by last seen or weakest (e.g. low correct ratio, high RT)
                .ThenBy(kvp => (double)kvp.Value.TimesCorrect / (kvp.Value.TimesCorrect + kvp.Value.TimesIncorrect)) // Then by accuracy
                .Take(_config.QuestionsPerBlock)
                .ToList();

            foreach (var candidate in candidates)
            {
                var factors = ParseFactKey(candidate.Key);
                reinforcementQuestions.Add(CreateQuestion(factors[0], factors[1]));
            }
            
            // If not enough candidates, fill with random known facts or from sequence
            int remaining = _config.QuestionsPerBlock - reinforcementQuestions.Count;
            if (remaining > 0 && _studentState.LearnedFacts.Any()){
                 var allLearnedKeys = _studentState.LearnedFacts.Keys.ToList();
                 for(int i=0; i < remaining && allLearnedKeys.Any(); i++){
                     var randomKey = allLearnedKeys[_random.Next(allLearnedKeys.Count)];
                     var factors = ParseFactKey(randomKey);
                     reinforcementQuestions.Add(CreateQuestion(factors[0], factors[1]));
                 }
            }

            return reinforcementQuestions.Take(_config.QuestionsPerBlock).OrderBy(q => _random.Next()).ToList();
        }

        private bool IsDueForReview(FactRecord record)
        {
            if (record.TimesCorrect == 0 && record.TimesIncorrect > 0) return true; // Always review if only incorrect
            if (record.TimesCorrect == 0) return false; // Not seen or only incorrect no spacing interval applies yet

            // Determine current interval based on TimesCorrect (simplified Leitner system logic)
            // More sophisticated: use record.MasteryLevel or similar concept
            int intervalIndex = Math.Min(record.TimesCorrect -1, _config.SpacingIntervals.Length - 1);
            long dueTime = record.LastSeen + _config.SpacingIntervals[intervalIndex];
            return DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() >= dueTime;
        }

        private Question CreateQuestion(int factor1, int factor2)
        {
            // Randomize factor order for presentation if desired, but key is standardized
            if (_random.NextDouble() < _config.RandomizeWindow) { // Using RandomizeWindow for another purpose here - to swap factors sometimes
                var temp = factor1;
                factor1 = factor2;
                factor2 = temp;
            }

            return new Question
            {
                Id = Guid.NewGuid().ToString(),
                Factors = new int[] { factor1, factor2 },
                Answer = factor1 * factor2
                // TimeStarted will be set when block is finalized
            };
        }
        
        private int[] ParseFactKey(string factKey)
        {
            return factKey.Split('x').Select(int.Parse).ToArray();
        }

        public Task<MasteryCheckResult> CheckMastery()
        {
            if (_studentState == null) throw new InvalidOperationException("Initialize first.");

            // Define what constitutes "all facts". This can be complex.
            // Simplistic: All unique N x M combinations where N is from sequence and M is 0-MaxFactor
            HashSet<string> allPossibleFactKeys = new HashSet<string>();
            foreach (int seqVal in _config.Sequence.Distinct()) // Use distinct sequence values
            {
                for (int i = 0; i <= _config.MaxFactor; i++)
                {
                    allPossibleFactKeys.Add(GetFactKey(new int[] { seqVal, i }));
                }
            }
            int totalFacts = allPossibleFactKeys.Count;
            int masteredFactsCount = 0;
            List<double> masteredResponseTimes = new List<double>();

            foreach (var factKey in allPossibleFactKeys)
            {
                if (_studentState.LearnedFacts.TryGetValue(factKey, out FactRecord record))
                {
                    if (IsFactMastered(record))
                    {
                        masteredFactsCount++;
                        masteredResponseTimes.Add(record.AverageResponseTime);
                    }
                }
            }

            double averageSpeed = masteredResponseTimes.Any() ? masteredResponseTimes.Average() : 0;
            bool isMastered = masteredFactsCount == totalFacts && totalFacts > 0; 

            return Task.FromResult(new MasteryCheckResult
            {
                IsMastered = isMastered,
                MasteredFacts = masteredFactsCount,
                TotalFacts = totalFacts,
                AverageSpeed = averageSpeed
            });
        }

        private bool IsFactMastered(FactRecord record)
        {
            // Define mastery criteria, e.g.:
            // - At least N correct answers in a row (not tracked directly, but TimesCorrect vs TimesIncorrect helps)
            // - Low average response time
            // - Met spacing criteria (passed several review intervals successfully)
            // Simple criteria for now:
            const int minCorrectAnswersForMastery = 3;
            const int maxIncorrectAnswersForMastery = 0;
            // const double maxAverageResponseTimeForMastery = 3000; // e.g. 3 seconds

            return record.TimesCorrect >= minCorrectAnswersForMastery && 
                   record.TimesIncorrect <= maxIncorrectAnswersForMastery;
                   // && record.AverageResponseTime <= maxAverageResponseTimeForMastery;
        }
    }
} 