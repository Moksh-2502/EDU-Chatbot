using System.Linq;
using Characters;
using Consumables;
using Cysharp.Threading.Tasks;
using FluencySDK;
using FluencySDK.Unity;
using UnityEngine;
using SubwaySurfers;
using SharedCore.Analytics;
using FluencySDK.Data;
using FluencySDK.UI;
using FluencySDK.Events;

namespace EducationIntegration.QuestionResultProcessor
{
    /// <summary>
    /// Streak-based implementation of question result processor
    /// Correct answers: Give 2s shield, 5 in a row adds 1 life
    /// Incorrect answers: Remove buffs immediately, 5 in a row removes 1 life with temporary invincibility
    /// </summary>
    public class StreakBasedQuestionResultProcessor : MonoBehaviour, IQuestionResultProcessor
    {
        [Header("Configuration")] [SerializeField]
        private StreakConfiguration streakConfiguration;

        [Header("Dependencies")] [SerializeField]
        private ConsumableDatabase consumableDatabase;

        // References to game systems
        private CharacterInputController _characterController;
        private CharacterCollider _characterCollider;
        private ILootablesSpawner _powerUpSpawner;
        private BaseQuestionProvider _questionProvider;
        private TrackManager _trackManager;

        private void Awake()
        {
            Initialize();
        }

        private void Initialize()
        {
            _characterController = FindFirstObjectByType<CharacterInputController>(FindObjectsInactive.Include);
            if (_characterController == null)
            {
                Debug.LogError("[StreakBasedQuestionResultProcessor] CharacterInputController not found");
            }

            _characterCollider = FindFirstObjectByType<CharacterCollider>(FindObjectsInactive.Include);
            if (_characterCollider == null)
            {
                Debug.LogError("[StreakBasedQuestionResultProcessor] CharacterCollider not found");
            }

            _powerUpSpawner = FindFirstObjectByType<LootablesSpawner>(FindObjectsInactive.Include);
            if (_powerUpSpawner == null)
            {
                Debug.LogError("[StreakBasedQuestionResultProcessor] LootablesSpawner not found");
            }

            _questionProvider = BaseQuestionProvider.Instance;
            if (_questionProvider == null)
            {
                Debug.LogError("[StreakBasedQuestionResultProcessor] BaseQuestionProvider not found");
            }

            _trackManager = FindFirstObjectByType<TrackManager>(FindObjectsInactive.Include);
            if (_trackManager == null)
            {
                Debug.LogError("[StreakBasedQuestionResultProcessor] TrackManager not found");
            }

            if (consumableDatabase == null)
            {
                Debug.LogError("[StreakBasedQuestionResultProcessor] ConsumableDatabase not assigned");
            }

            if (streakConfiguration == null)
            {
                Debug.LogError("[StreakBasedQuestionResultProcessor] StreakConfiguration not assigned");
            }
        }

        public void ProcessQuestionResult(IQuestion question, UserAnswerSubmission userAnswerSubmission)
        {

            if (userAnswerSubmission.AnswerType == AnswerType.Correct)
            {
                ProcessCorrectAnswer(question);
            }
            else if(userAnswerSubmission.AnswerType == AnswerType.Incorrect)
            {
                ProcessIncorrectAnswer(question);
            }
        }

        private void ProcessCorrectAnswer(IQuestion question)
        {
            Debug.Log("[StreakBasedQuestionResultProcessor] Correct answer - giving shield buff");
            
            // Always give shield for correct answers
            GiveShieldBuff().Forget();

            // Show correct answer feedback with shield info
            ShowCorrectAnswerFeedback();

            // Check streak-based feedback and rewards
            if (_questionProvider?.StudentState != null && streakConfiguration != null)
            {
                var actualStreak = _questionProvider.StudentState.GetPersistentCorrectStreak();

                // log the current correct streak
                Debug.Log($"[StreakBasedQuestionResultProcessor] Current correct streak: {actualStreak}");

                // Check if we've reached the correct streak threshold for bonus life
                // Trigger when streak is exactly divisible by threshold (5, 10, 15, etc.)
                if (actualStreak > 0 && actualStreak % streakConfiguration.CorrectStreakThreshold == 0)
                {
                    AddLife();
                    ShowCompleteCorrectStreakFeedback();
                }
                else
                {
                    // Show progress indicator - use modulo to show progress within current cycle
                    var progressInCycle = actualStreak % streakConfiguration.CorrectStreakThreshold;
                    if (streakConfiguration.EnableCorrectStreakVisualIndicator && progressInCycle >=
                        streakConfiguration.CorrectStreakVisualIndicatorThreshold)
                    {
                        ShowCorrectStreakProgressFeedback(progressInCycle);
                    }
                }
            }
        }

        private void ProcessIncorrectAnswer(IQuestion question)
        {
            Debug.Log("[StreakBasedQuestionResultProcessor] Incorrect answer - removing buffs");

            // Always remove buffs immediately for incorrect answers
            RemoveAllActiveBuffs();

            // Show incorrect answer feedback
            ShowIncorrectAnswerFeedback();

            // Check if we've reached the incorrect streak threshold for life penalty
            if (_questionProvider?.StudentState != null && streakConfiguration != null)
            {
                var actualStreak = _questionProvider.StudentState.GetPersistentIncorrectStreak();

                // log the current incorrect streak
                Debug.Log($"[StreakBasedQuestionResultProcessor] Current incorrect streak: {actualStreak}");

                // Check if we've reached the incorrect streak threshold for life penalty
                // Trigger when streak is exactly divisible by threshold (5, 10, 15, etc.)
                if (actualStreak > 0 && actualStreak % streakConfiguration.IncorrectStreakThreshold == 0)
                {
                    TakeLifeFromStreak();
                    ShowCompleteIncorrectStreakFeedback();
                }
                else
                {
                    // Show buff removed feedback (only when we're not removing a life)
                    var feedbackArgs = new QuestionFeedbackEventArgs(FeedbackType.IncorrectPenalty, "Buffs Removed!");
                    IQuestionFeedbackDisplayer.Instance?.DisplayFeedback(feedbackArgs);
                    
                    // Show warning indicator - use modulo to show progress within current cycle
                    var progressInCycle = actualStreak % streakConfiguration.IncorrectStreakThreshold;
                    if (streakConfiguration.EnableIncorrectStreakVisualIndicator && progressInCycle >=
                        streakConfiguration.IncorrectStreakVisualIndicatorThreshold)
                    {
                        ShowIncorrectStreakWarningFeedback(progressInCycle);
                    }
                }
            }
        }

        private void ShowCorrectAnswerFeedback()
        {
            if (streakConfiguration == null) return;

            var feedbackArgs = new QuestionFeedbackEventArgs(
                FeedbackType.CorrectWord,
                "Correct!",
                null);

            IQuestionFeedbackDisplayer.Instance?.DisplayFeedback(feedbackArgs);

            var shieldIcon = GetShieldIcon();
            var feedbackText = $"+{streakConfiguration.CorrectAnswerShieldDuration}<size=75%>s</size> shield";

            feedbackArgs = new QuestionFeedbackEventArgs(FeedbackType.CorrectReward, feedbackText, shieldIcon);
            IQuestionFeedbackDisplayer.Instance?.DisplayFeedback(feedbackArgs);
        }

        private void ShowIncorrectAnswerFeedback()
        {
            if (streakConfiguration == null) return;
            // Show incorrect answer feedback
            var feedbackArgs = new QuestionFeedbackEventArgs(
                FeedbackType.IncorrectWord,
                "Try Again!",
                null);
            IQuestionFeedbackDisplayer.Instance?.DisplayFeedback(feedbackArgs);
        }

        private void ShowCorrectStreakProgressFeedback(int currentStreak)
        {
            if (streakConfiguration == null) return;

            var feedbackText = $"Streak {currentStreak}/{streakConfiguration.CorrectStreakThreshold}";
            var feedbackArgs = new QuestionFeedbackEventArgs(FeedbackType.CorrectStreak, feedbackText, null);
            IQuestionFeedbackDisplayer.Instance?.DisplayFeedback(feedbackArgs);
        }

        private void ShowCompleteCorrectStreakFeedback()
        {
            var lifeIcon = GetLifeIcon();
            var feedbackArgs = new QuestionFeedbackEventArgs(FeedbackType.CompleteCorrectStreak, "+1 Life", lifeIcon);
            IQuestionFeedbackDisplayer.Instance?.DisplayFeedback(feedbackArgs);
        }

        private void ShowIncorrectStreakWarningFeedback(int currentStreak)
        {
            if (streakConfiguration == null) return;

            var feedbackText = $"Incorrect Streak {currentStreak}/{streakConfiguration.IncorrectStreakThreshold}";
            var feedbackArgs = new QuestionFeedbackEventArgs(FeedbackType.IncorrectStreak, feedbackText, null);
            IQuestionFeedbackDisplayer.Instance?.DisplayFeedback(feedbackArgs);
        }

        private void ShowCompleteIncorrectStreakFeedback()
        {
            var lifeIcon = GetLifeIcon();
            var feedbackArgs = new QuestionFeedbackEventArgs(FeedbackType.CompleteIncorrectStreak, "-1 Life", lifeIcon);
            IQuestionFeedbackDisplayer.Instance?.DisplayFeedback(feedbackArgs);
        }

        private Sprite GetShieldIcon()
        {
            if (consumableDatabase == null) return null;

            var shieldConsumable = consumableDatabase.consumbales
                .FirstOrDefault(c => c.GetConsumableType() == Consumable.ConsumableType.SHIELD);

            return shieldConsumable?.icon;
        }

        private Sprite GetLifeIcon()
        {
            if (consumableDatabase == null) return null;

            var lifeConsumable = consumableDatabase.consumbales
                .FirstOrDefault(c => c.GetConsumableType() == Consumable.ConsumableType.EXTRALIFE);

            return lifeConsumable?.icon;
        }

        private async UniTask GiveShieldBuff()
        {
            if (_characterController == null || consumableDatabase == null || _powerUpSpawner == null ||
                streakConfiguration == null)
            {
                Debug.LogError(
                    "[StreakBasedQuestionResultProcessor] Cannot give shield buff: Missing required components");
                return;
            }

            // Find shield consumable in database
            var shieldConsumable = consumableDatabase.consumbales
                .FirstOrDefault(c => c.GetConsumableType() == Consumable.ConsumableType.SHIELD);

            if (shieldConsumable == null)
            {
                Debug.LogError("[StreakBasedQuestionResultProcessor] Shield consumable not found in database");
                return;
            }

            // Spawn and apply the shield consumable
            var spawnedConsumable = await _powerUpSpawner.SpawnAsync<Consumable>(
                shieldConsumable.gameObject,
                Vector3.one * 9999,
                Quaternion.identity);

            if (spawnedConsumable != null)
            {
                // Override the duration to match our configuration
                spawnedConsumable.duration = streakConfiguration.CorrectAnswerShieldDuration;

                // Apply the consumable to the character
                _characterController.UseConsumable(spawnedConsumable);

                Debug.Log(
                    $"[StreakBasedQuestionResultProcessor] Applied shield buff for {streakConfiguration.CorrectAnswerShieldDuration}s.");
            }
            else
            {
                Debug.LogError("[StreakBasedQuestionResultProcessor] Failed to spawn shield consumable");
            }
        }

        private void AddLife()
        {
            if (_characterController == null)
            {
                Debug.LogError(
                    "[StreakBasedQuestionResultProcessor] Cannot add life: CharacterInputController not found");
                return;
            }

            if (IPlayerStateProvider.Instance.ChangeLives(1))
            {
                Debug.Log(
                    $"[StreakBasedQuestionResultProcessor] Added life. Current lives: {IPlayerStateProvider.Instance.CurrentLives}");

                // TODO: Add visual/audio feedback for gaining a life from streak
            }
            else
            {
                // If at max life, could give coins instead (like ExtraLife consumable does)
                Debug.Log("[StreakBasedQuestionResultProcessor] At max life, gave 10 coins instead");
            }
        }

        private void TakeLifeFromStreak()
        {
            if (_characterCollider == null || streakConfiguration == null)
            {
                Debug.LogError("[StreakBasedQuestionResultProcessor] Cannot take life: Missing required components");
                return;
            }

            // Take life with temporary invincibility (ignores normal invincibility checks)
            _characterCollider.TakeLife(
                "Incorrect Answer Streak",
                ignoreInvincibility: true);

            Debug.Log($"[StreakBasedQuestionResultProcessor] Took life from incorrect streak");
        }

        private void RemoveAllActiveBuffs()
        {
            if (_characterController == null)
            {
                Debug.LogError(
                    "[StreakBasedQuestionResultProcessor] Cannot remove buffs: CharacterInputController not found");
                return;
            }

            // Clean all active consumables (this removes all buffs)
            _characterController.CleanConsumable();

            Debug.Log("[StreakBasedQuestionResultProcessor] Removed all active buffs");
        }
    }
}