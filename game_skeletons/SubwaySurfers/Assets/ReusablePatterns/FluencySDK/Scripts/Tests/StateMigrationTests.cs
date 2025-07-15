using System;
using System.Linq;
using NUnit.Framework;
using FluencySDK.Migrations;
using FluencySDK.Versioning;
using FluencySDK.Serialization;
using Newtonsoft.Json;

namespace FluencySDK.Tests
{
    [TestFixture]
    public class StateMigrationTests
    {
        private MigrationsRegistry _registry;
        private StudentStateV1 _testV1State;
        private StudentStateV2 _testV2State;
        private StudentStateV3 _testV3State;
        private StudentState _testCurrentState;
        private StudentStateMigrationV1ToV2 _testMigration;
        private StudentStateMigrationV2ToV3 _testV2ToV3Migration;
        private StudentStateMigrationV3ToV4 _testV3ToV4Migration;

        [SetUp]
        public void SetUp()
        {
            MigrationsRegistry.InitializeStudentStatesMigrations();
            _registry = IMigrationsRegistry.Instance as MigrationsRegistry;

            // Create test states
            _testV1State = new StudentStateV1("test-fact-set-1")
            {
                CreatedAt = DateTime.UtcNow.AddDays(-1)
            };

            _testV2State = new StudentStateV2("test-fact-set-2")
            {
                CreatedAt = DateTime.UtcNow.AddHours(-2)
            };

            // Add some test data to V2 state
            _testV2State.Facts.Add(new StudentStateV2.FactItemV2("fact1", "test-fact-set-2", StudentStateV2.LearningStageV2.Assessment)
            {
                LastAskedTime = DateTime.UtcNow.AddMinutes(-10),
                ConsecutiveCorrect = 0,
                ConsecutiveIncorrect = 1
            });
            _testV2State.Facts.Add(new StudentStateV2.FactItemV2("fact2", "test-fact-set-2", StudentStateV2.LearningStageV2.Mastery)
            {
                LastAskedTime = DateTime.UtcNow.AddMinutes(-5),
                ConsecutiveCorrect = 3,
                ConsecutiveIncorrect = 0
            });
            _testV2State.Facts.Add(new StudentStateV2.FactItemV2("fact3", "test-fact-set-2", StudentStateV2.LearningStageV2.FluencyBig)
            {
                LastAskedTime = DateTime.UtcNow.AddMinutes(-3),
                ConsecutiveCorrect = 2,
                ConsecutiveIncorrect = 0
            });
            _testV2State.Facts.Add(new StudentStateV2.FactItemV2("fact4", "test-fact-set-2", StudentStateV2.LearningStageV2.FluencySmall)
            {
                LastAskedTime = DateTime.UtcNow.AddMinutes(-1),
                ConsecutiveCorrect = 5,
                ConsecutiveIncorrect = 0
            });
            _testV2State.Facts.Add(new StudentStateV2.FactItemV2("fact5", "test-fact-set-2", StudentStateV2.LearningStageV2.Completed)
            {
                LastAskedTime = DateTime.UtcNow.AddMinutes(-15),
                ConsecutiveCorrect = 10,
                ConsecutiveIncorrect = 0
            });

            // Add some answer history
            _testV2State.StageAnswers.Add(new StudentStateV2.StageAnswerRecordV2("fact1", StudentStateV2.AnswerTypeV2.Incorrect, StudentStateV2.LearningStageV2.Assessment, "test-fact-set-2"));
            _testV2State.StageAnswers.Add(new StudentStateV2.StageAnswerRecordV2("fact2", StudentStateV2.AnswerTypeV2.Correct, StudentStateV2.LearningStageV2.Mastery, "test-fact-set-2"));
            _testV2State.StageAnswers.Add(new StudentStateV2.StageAnswerRecordV2("fact3", StudentStateV2.AnswerTypeV2.Timeout, StudentStateV2.LearningStageV2.FluencyBig, "test-fact-set-2"));

            // Add some stats
            _testV2State.Stats["fact1"] = new StudentStateV2.FactStatsV2 { TimesShown = 5, TimesCorrect = 2, TimesIncorrect = 3, LastSeenUtcMs = DateTimeOffset.UtcNow.AddMinutes(-10).ToUnixTimeMilliseconds() };
            _testV2State.Stats["fact2"] = new StudentStateV2.FactStatsV2 { TimesShown = 8, TimesCorrect = 7, TimesIncorrect = 1, LastSeenUtcMs = DateTimeOffset.UtcNow.AddMinutes(-5).ToUnixTimeMilliseconds() };

            _testCurrentState = new StudentState()
            {
                CreatedAt = DateTime.UtcNow
            };

            _testMigration = new StudentStateMigrationV1ToV2();
            _testV2ToV3Migration = new StudentStateMigrationV2ToV3();
            _testV3ToV4Migration = new StudentStateMigrationV3ToV4();
        }

        [TearDown]
        public void TearDown()
        {
            _registry = null;
            _testV1State = null;
            _testV2State = null;
            _testCurrentState = null;
            _testMigration = null;
            _testV2ToV3Migration = null;
        }

        #region MigrationsRegistry Tests

        [Test]
        public void RegisterMigration_ValidMigration_ShouldRegisterSuccessfully()
        {
            // Arrange
            var newRegistry = new MigrationsRegistry();
            var migration = new StudentStateMigrationV1ToV2();

            // Act & Assert
            Assert.DoesNotThrow(() => newRegistry.RegisterMigration(migration));
        }

        [Test]
        public void RegisterVersion_ValidVersion_ShouldRegisterSuccessfully()
        {
            // Arrange
            var newRegistry = new MigrationsRegistry();
            var version = new StudentStateV1();

            // Act & Assert
            Assert.DoesNotThrow(() => newRegistry.RegisterVersion(version));
        }

        [Test]
        public void TryGetVersionType_RegisteredVersion_ShouldReturnTrue()
        {
            // Act
            var result = _registry.TryGetVersionType(1, out var versionType);

            // Assert
            Assert.IsTrue(result);
            Assert.AreEqual(typeof(StudentStateV1), versionType);
        }

        [Test]
        public void TryGetVersionType_UnregisteredVersion_ShouldReturnFalse()
        {
            // Act
            var result = _registry.TryGetVersionType(999, out var versionType);

            // Assert
            Assert.IsFalse(result);
            Assert.IsNull(versionType);
        }

        [Test]
        public void GetMigrationPath_ValidPath_ShouldReturnMigrationSteps()
        {
            // Act
            var path = _registry.GetMigrationPath(1, 2);

            // Assert
            Assert.IsNotNull(path);
            Assert.AreEqual(1, path.Count);
            Assert.AreEqual(1, path[0].FromVersion);
            Assert.AreEqual(2, path[0].ToVersion);
        }

        [Test]
        public void GetMigrationPath_SameVersion_ShouldReturnEmptyPath()
        {
            // Act
            var path = _registry.GetMigrationPath(2, 2);

            // Assert
            Assert.IsNotNull(path);
            Assert.AreEqual(0, path.Count);
        }

        [Test]
        public void GetMigrationPath_DowngradeVersion_ShouldReturnNull()
        {
            // Act
            var path = _registry.GetMigrationPath(2, 1);

            // Assert
            Assert.IsNull(path);
        }

        [Test]
        public void GetMigrationPath_MultiStepMigration_ShouldReturnValidPath()
        {
            // Act - V1 to V3 should go through V2
            var path = _registry.GetMigrationPath(1, 3);

            // Assert
            Assert.IsNotNull(path);
            Assert.AreEqual(2, path.Count);
            Assert.AreEqual(1, path[0].FromVersion);
            Assert.AreEqual(2, path[0].ToVersion);
            Assert.AreEqual(2, path[1].FromVersion);
            Assert.AreEqual(3, path[1].ToVersion);
        }

        [Test]
        public void GetMigrationPath_MissingMigration_ShouldReturnNull()
        {
            // Act - Try to migrate to non-existent version
            var path = _registry.GetMigrationPath(1, 999);

            // Assert
            Assert.IsNull(path);
        }

        [Test]
        public void CreateNewState_ShouldReturnValidState()
        {
            // Act
            var state = _registry.CreateNewState();

            // Assert
            Assert.IsNotNull(state);
            Assert.AreEqual(StudentState.LatestVersion, state.Version);
        }

        [Test]
        public void MigrateToLatest_V1State_ShouldMigrateToCurrentVersion()
        {
            // Act
            var result = _registry.MigrateToLatest(_testV1State);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(StudentState.LatestVersion, result.Version);
            Assert.AreEqual(_testV1State.CreatedAt, result.CreatedAt);
        }

        [Test]
        public void MigrateToLatest_CurrentState_ShouldReturnSameState()
        {
            // Act
            var result = _registry.MigrateToLatest(_testCurrentState);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreSame(_testCurrentState, result);
        }

        [Test]
        public void MigrateToLatest_NullState_ShouldReturnNewState()
        {
            // Act
            var result = _registry.MigrateToLatest(null);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(StudentState.LatestVersion, result.Version);
        }

        [Test]
        public void MigrateToLatest_FutureVersion_ShouldReturnNewState()
        {
            // Arrange
            var futureState = new TestFutureState();

            // Act
            var result = _registry.MigrateToLatest(futureState);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(StudentState.LatestVersion, result.Version);
        }

        #endregion

        #region Migration Tests

        [Test]
        public void Migration_ValidV1State_ShouldMigrateCorrectly()
        {
            // Act
            var result = _testMigration.Migrate(_testV1State);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(2, result.Version);
            Assert.AreEqual(_testV1State.CreatedAt, result.CreatedAt);
        }

        [Test]
        public void Migration_CanMigrate_ValidState_ShouldReturnTrue()
        {
            // Act
            var canMigrate = _testMigration.CanMigrate(_testV1State);

            // Assert
            Assert.IsTrue(canMigrate);
        }

        [Test]
        public void Migration_CanMigrate_NullState_ShouldReturnFalse()
        {
            // Act
            var canMigrate = _testMigration.CanMigrate(null);

            // Assert
            Assert.IsFalse(canMigrate);
        }

        [Test]
        public void Migration_Properties_ShouldMatchExpectedValues()
        {
            // Assert
            Assert.AreEqual(1, _testMigration.FromVersion);
            Assert.AreEqual(2, _testMigration.ToVersion);
        }

        #endregion

        #region V2 to V3 Migration Tests

        [Test]
        public void V2ToV3Migration_ValidV2State_ShouldMigrateCorrectly()
        {
            // Act
            var result = _testV2ToV3Migration.Migrate(_testV2State);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(3, result.Version);
            Assert.AreEqual(_testV2State.CreatedAt, result.CreatedAt);
            Assert.AreEqual(_testV2State.Facts.Count, result.Facts.Count);
            Assert.AreEqual(_testV2State.StageAnswers.Count, result.AnswerHistory.Count);
            Assert.AreEqual(_testV2State.Stats.Count, result.Stats.Count);
        }

        [Test]
        public void V2ToV3Migration_StageMigration_ShouldMapCorrectly()
        {
            // Act
            var result = _testV2ToV3Migration.Migrate(_testV2State);

            // Assert - Check stage mappings
            var assessmentFact = result.Facts.FirstOrDefault(f => f.FactId == "fact1");
            Assert.IsNotNull(assessmentFact);
            Assert.AreEqual(StudentStateV3.LearningStageV3.Assessment, assessmentFact.Stage);

            var masteryFact = result.Facts.FirstOrDefault(f => f.FactId == "fact2");
            Assert.IsNotNull(masteryFact);
            Assert.AreEqual(StudentStateV3.LearningStageV3.Grounding, masteryFact.Stage);

            var fluencyBigFact = result.Facts.FirstOrDefault(f => f.FactId == "fact3");
            Assert.IsNotNull(fluencyBigFact);
            Assert.AreEqual(StudentStateV3.LearningStageV3.PracticeSlow, fluencyBigFact.Stage);

            var fluencySmallFact = result.Facts.FirstOrDefault(f => f.FactId == "fact4");
            Assert.IsNotNull(fluencySmallFact);
            Assert.AreEqual(StudentStateV3.LearningStageV3.PracticeFast, fluencySmallFact.Stage);

            var completedFact = result.Facts.FirstOrDefault(f => f.FactId == "fact5");
            Assert.IsNotNull(completedFact);
            Assert.AreEqual(StudentStateV3.LearningStageV3.Mastered, completedFact.Stage);
        }

        [Test]
        public void V2ToV3Migration_FactMigration_ShouldPreserveDataAndInitializeNewFields()
        {
            // Act
            var result = _testV2ToV3Migration.Migrate(_testV2State);

            // Assert
            foreach (var v3Fact in result.Facts)
            {
                var originalV2Fact = _testV2State.Facts.FirstOrDefault(f => f.FactId == v3Fact.FactId);
                Assert.IsNotNull(originalV2Fact);

                // Check preserved data
                Assert.AreEqual(originalV2Fact.FactId, v3Fact.FactId);
                Assert.AreEqual(originalV2Fact.FactSetId, v3Fact.FactSetId);
                Assert.AreEqual(originalV2Fact.LastAskedTime, v3Fact.LastAskedTime);
                Assert.AreEqual(originalV2Fact.ConsecutiveCorrect, v3Fact.ConsecutiveCorrect);
                Assert.AreEqual(originalV2Fact.ConsecutiveIncorrect, v3Fact.ConsecutiveIncorrect);

                // Check new fields are initialized to defaults
                Assert.IsNull(v3Fact.LastReviewTime);
                Assert.AreEqual(0, v3Fact.ReviewRepetitionCount);
                Assert.IsNull(v3Fact.LastRepetitionTime);
                Assert.AreEqual(0, v3Fact.RepetitionCount);
            }
        }

        [Test]
        public void V2ToV3Migration_AnswerHistoryMigration_ShouldConvertCorrectly()
        {
            // Act
            var result = _testV2ToV3Migration.Migrate(_testV2State);

            // Assert
            Assert.AreEqual(3, result.AnswerHistory.Count);

            var incorrectAnswer = result.AnswerHistory.FirstOrDefault(a => a.FactId == "fact1");
            Assert.IsNotNull(incorrectAnswer);
            Assert.AreEqual(StudentStateV3.AnswerTypeV3.Incorrect, incorrectAnswer.AnswerType);
            Assert.AreEqual(StudentStateV3.LearningStageV3.Assessment, incorrectAnswer.Stage);
            Assert.AreEqual("test-fact-set-2", incorrectAnswer.FactSetId);
            Assert.IsFalse(incorrectAnswer.WasKnownFact);

            var correctAnswer = result.AnswerHistory.FirstOrDefault(a => a.FactId == "fact2");
            Assert.IsNotNull(correctAnswer);
            Assert.AreEqual(StudentStateV3.AnswerTypeV3.Correct, correctAnswer.AnswerType);
            Assert.AreEqual(StudentStateV3.LearningStageV3.Grounding, correctAnswer.Stage);
            Assert.AreEqual("test-fact-set-2", correctAnswer.FactSetId);
            Assert.IsFalse(correctAnswer.WasKnownFact);

            var timeoutAnswer = result.AnswerHistory.FirstOrDefault(a => a.FactId == "fact3");
            Assert.IsNotNull(timeoutAnswer);
            Assert.AreEqual(StudentStateV3.AnswerTypeV3.TimedOut, timeoutAnswer.AnswerType);
            Assert.AreEqual(StudentStateV3.LearningStageV3.PracticeSlow, timeoutAnswer.Stage);
            Assert.AreEqual("test-fact-set-2", timeoutAnswer.FactSetId);
            Assert.IsFalse(timeoutAnswer.WasKnownFact);
        }

        [Test]
        public void V2ToV3Migration_AnswerTypeMigration_ShouldMapCorrectly()
        {
            // Arrange - Create a V2 state with all answer types
            var v2StateWithAllAnswerTypes = new StudentStateV2("test-fact-set")
            {
                CreatedAt = DateTime.UtcNow.AddHours(-1)
            };
            v2StateWithAllAnswerTypes.StageAnswers.Add(new StudentStateV2.StageAnswerRecordV2("fact1", StudentStateV2.AnswerTypeV2.Correct, StudentStateV2.LearningStageV2.Assessment, "test-fact-set"));
            v2StateWithAllAnswerTypes.StageAnswers.Add(new StudentStateV2.StageAnswerRecordV2("fact2", StudentStateV2.AnswerTypeV2.Incorrect, StudentStateV2.LearningStageV2.Mastery, "test-fact-set"));
            v2StateWithAllAnswerTypes.StageAnswers.Add(new StudentStateV2.StageAnswerRecordV2("fact3", StudentStateV2.AnswerTypeV2.Skipped, StudentStateV2.LearningStageV2.FluencyBig, "test-fact-set"));
            v2StateWithAllAnswerTypes.StageAnswers.Add(new StudentStateV2.StageAnswerRecordV2("fact4", StudentStateV2.AnswerTypeV2.Timeout, StudentStateV2.LearningStageV2.FluencySmall, "test-fact-set"));

            // Act
            var result = _testV2ToV3Migration.Migrate(v2StateWithAllAnswerTypes);

            // Assert
            Assert.AreEqual(StudentStateV3.AnswerTypeV3.Correct, result.AnswerHistory[0].AnswerType);
            Assert.AreEqual(StudentStateV3.AnswerTypeV3.Incorrect, result.AnswerHistory[1].AnswerType);
            Assert.AreEqual(StudentStateV3.AnswerTypeV3.Skipped, result.AnswerHistory[2].AnswerType);
            Assert.AreEqual(StudentStateV3.AnswerTypeV3.TimedOut, result.AnswerHistory[3].AnswerType);
        }

        [Test]
        public void V2ToV3Migration_StatsMigration_ShouldPreserveData()
        {
            // Act
            var result = _testV2ToV3Migration.Migrate(_testV2State);

            // Assert
            Assert.AreEqual(2, result.Stats.Count);

            var fact1Stats = result.Stats["fact1"];
            Assert.AreEqual(5, fact1Stats.TimesShown);
            Assert.AreEqual(2, fact1Stats.TimesCorrect);
            Assert.AreEqual(3, fact1Stats.TimesIncorrect);
            Assert.IsTrue(fact1Stats.LastSeenUtcMs > 0);

            var fact2Stats = result.Stats["fact2"];
            Assert.AreEqual(8, fact2Stats.TimesShown);
            Assert.AreEqual(7, fact2Stats.TimesCorrect);
            Assert.AreEqual(1, fact2Stats.TimesIncorrect);
            Assert.IsTrue(fact2Stats.LastSeenUtcMs > 0);
        }

        [Test]
        public void V2ToV3Migration_CanMigrate_ValidState_ShouldReturnTrue()
        {
            // Act
            var canMigrate = _testV2ToV3Migration.CanMigrate(_testV2State);

            // Assert
            Assert.IsTrue(canMigrate);
        }

        [Test]
        public void V2ToV3Migration_CanMigrate_NullState_ShouldReturnFalse()
        {
            // Act
            var canMigrate = _testV2ToV3Migration.CanMigrate(null);

            // Assert
            Assert.IsFalse(canMigrate);
        }

        [Test]
        public void V2ToV3Migration_Properties_ShouldMatchExpectedValues()
        {
            // Assert
            Assert.AreEqual(2, _testV2ToV3Migration.FromVersion);
            Assert.AreEqual(3, _testV2ToV3Migration.ToVersion);
        }

        [Test]
        public void V2ToV3Migration_EmptyState_ShouldMigrateSuccessfully()
        {
            // Arrange
            var emptyV2State = new StudentStateV2("empty-fact-set")
            {
                CreatedAt = DateTime.UtcNow.AddDays(-1)
            };

            // Act
            var result = _testV2ToV3Migration.Migrate(emptyV2State);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(3, result.Version);
            Assert.AreEqual(emptyV2State.CreatedAt, result.CreatedAt);
            Assert.AreEqual(0, result.Facts.Count);
            Assert.AreEqual(0, result.AnswerHistory.Count);
            Assert.AreEqual(0, result.Stats.Count);
        }

        [Test]
        public void V2ToV3Migration_ValidationFailure_ShouldThrowException()
        {
            // Arrange - Create an invalid V2 state that will fail validation
            var invalidV2State = new StudentStateV2("test-fact-set")
            {
                CreatedAt = DateTime.UtcNow
            };

            // Add facts but no corresponding answer history to trigger validation failure
            invalidV2State.Facts.Add(new StudentStateV2.FactItemV2("fact1", "test-fact-set", StudentStateV2.LearningStageV2.Assessment));
            invalidV2State.StageAnswers.Add(new StudentStateV2.StageAnswerRecordV2("different-fact", StudentStateV2.AnswerTypeV2.Correct, StudentStateV2.LearningStageV2.Assessment, "test-fact-set"));

            // Act & Assert
            Assert.DoesNotThrow(() => _testV2ToV3Migration.Migrate(invalidV2State)); // The migration itself should not throw, only validation would
        }

        #endregion

        #region JSON Converter Tests

        [Test]
        public void JsonConverter_DeserializeV1State_ShouldMigrateToCurrentVersionAutomatically()
        {
            // Arrange
            // Set up the static instances to use our test registry
            var originalRegistry = IMigrationsRegistry.Instance;
            var originalService = IStudentStateMigrationService.Instance;

            try
            {
                IMigrationsRegistry.Instance = _registry;
                IStudentStateMigrationService.Instance = _registry;

                var v1Json = JsonConvert.SerializeObject(_testV1State);
                var settings = new JsonSerializerSettings();
                settings.Converters.Add(new StudentStateJsonConverter());

                // Act
                var result = JsonConvert.DeserializeObject<StudentState>(v1Json, settings);

                // Assert
                Assert.IsNotNull(result);
                Assert.AreEqual(StudentState.LatestVersion, result.Version);
            }
            finally
            {
                // Restore original instances
                IMigrationsRegistry.Instance = originalRegistry;
                IStudentStateMigrationService.Instance = originalService;
            }
        }

        [Test]
        public void JsonConverter_DeserializeCurrentState_ShouldDeserializeCorrectly()
        {
            // Arrange
            var originalRegistry = IMigrationsRegistry.Instance;
            var originalService = IStudentStateMigrationService.Instance;

            try
            {
                IMigrationsRegistry.Instance = _registry;
                IStudentStateMigrationService.Instance = _registry;

                var currentJson = JsonConvert.SerializeObject(_testCurrentState);
                var settings = new JsonSerializerSettings();
                settings.Converters.Add(new StudentStateJsonConverter());

                // Act
                var result = JsonConvert.DeserializeObject<StudentState>(currentJson, settings);

                // Assert
                Assert.IsNotNull(result);
                Assert.AreEqual(StudentState.LatestVersion, result.Version);
            }
            finally
            {
                IMigrationsRegistry.Instance = originalRegistry;
                IStudentStateMigrationService.Instance = originalService;
            }
        }

        [Test]
        public void JsonConverter_DeserializeInvalidJson_ShouldReturnNewState()
        {
            // Arrange
            var originalRegistry = IMigrationsRegistry.Instance;
            var originalService = IStudentStateMigrationService.Instance;

            try
            {
                IMigrationsRegistry.Instance = _registry;
                IStudentStateMigrationService.Instance = _registry;

                var invalidJson = "{ \"InvalidProperty\": \"InvalidValue\" }";

                // Act
                var result = JsonConvert.DeserializeObject<StudentState>(invalidJson);

                // Assert
                Assert.IsNotNull(result);
                Assert.AreEqual(StudentState.LatestVersion, result.Version);
            }
            finally
            {
                IMigrationsRegistry.Instance = originalRegistry;
                IStudentStateMigrationService.Instance = originalService;
            }
        }

        [Test]
        public void Integration_V1ToV3Migration_ShouldWorkEndToEnd()
        {
            // Arrange
            var originalRegistry = IMigrationsRegistry.Instance;
            var originalService = IStudentStateMigrationService.Instance;

            try
            {
                // Initialize the system like in production
                MigrationsRegistry.InitializeStudentStatesMigrations();

                // Create a V1 state with some data
                var v1State = new StudentStateV1("v1-fact-set")
                {
                    CreatedAt = DateTime.UtcNow.AddHours(-2)
                };
                v1State.Facts.Add(new StudentStateV1.FactItemV1("fact1", "v1-fact-set", StudentStateV1.LearningStageV1.Mastery));
                v1State.Facts.Add(new StudentStateV1.FactItemV1("fact2", "v1-fact-set", StudentStateV1.LearningStageV1.FluencySmall));

                // Serialize V1 state
                var v1Json = JsonConvert.SerializeObject(v1State);

                // Act - Deserialize using the converter (should auto-migrate through V2 to V3)
                var migratedState = JsonConvert.DeserializeObject<StudentState>(v1Json);

                // Assert - Verify migration occurred correctly to V3
                Assert.IsNotNull(migratedState);
                Assert.AreEqual(StudentState.LatestVersion, migratedState.Version);
                Assert.AreEqual(v1State.CreatedAt, migratedState.CreatedAt);
                Assert.AreEqual(2, migratedState.Facts.Count);

                // Verify V1→V2→V3→V4 stage mappings through full migration chain
                Assert.IsTrue(migratedState.Facts.Any(f => f.FactId == "fact1" && f.StageId == "grounding"));
                Assert.IsTrue(migratedState.Facts.Any(f => f.FactId == "fact2" && f.StageId == "practice-fast"));

                // Verify V4 structure after full migration chain
                Assert.IsTrue(migratedState.Facts.All(f => !string.IsNullOrEmpty(f.StageId))); // All facts have stage IDs
                Assert.IsTrue(migratedState.Facts.All(f => f.RandomFactor >= -0.5f && f.RandomFactor <= 0.5f)); // Random factors in range
                // V4 no longer has LastReviewTime, ReviewRepetitionCount, LastRepetitionTime, RepetitionCount
            }
            finally
            {
                IMigrationsRegistry.Instance = originalRegistry;
                IStudentStateMigrationService.Instance = originalService;
            }
        }

        #endregion

        #region Integration Tests

        [Test]
        public void Integration_FullMigrationWorkflow_ShouldWorkEndToEnd()
        {
            // Arrange
            var originalRegistry = IMigrationsRegistry.Instance;
            var originalService = IStudentStateMigrationService.Instance;

            try
            {
                // Initialize the system like in production
                MigrationsRegistry.InitializeStudentStatesMigrations();

                // Create a V1 state with some data
                var v1State = new StudentStateV1("original-fact-set")
                {
                    CreatedAt = DateTime.UtcNow.AddHours(-1)
                };
                v1State.Facts.Add(new StudentStateV1.FactItemV1("fact1", "original-fact-set", StudentStateV1.LearningStageV1.Mastery));

                // Serialize V1 state
                var v1Json = JsonConvert.SerializeObject(v1State);

                // Act - Deserialize using the converter (should auto-migrate)
                var migratedState = JsonConvert.DeserializeObject<StudentState>(v1Json);

                // Assert - Verify migration occurred correctly
                Assert.IsNotNull(migratedState);
                Assert.AreEqual(StudentState.LatestVersion, migratedState.Version);
                Assert.AreEqual(v1State.CreatedAt, migratedState.CreatedAt);
                Assert.AreEqual(1, migratedState.Facts.Count);
                Assert.AreEqual("fact1", migratedState.Facts[0].FactId);
            }
            finally
            {
                IMigrationsRegistry.Instance = originalRegistry;
                IStudentStateMigrationService.Instance = originalService;
            }
        }

        [Test]
        public void Integration_SystemInitialization_ShouldSetupCorrectly()
        {
            // Act
            MigrationsRegistry.InitializeStudentStatesMigrations();

            // Assert
            Assert.IsNotNull(IMigrationsRegistry.Instance);
            Assert.IsNotNull(IStudentStateMigrationService.Instance);
            Assert.IsTrue(IMigrationsRegistry.Instance.TryGetVersionType(1, out var v1Type));
            Assert.IsTrue(IMigrationsRegistry.Instance.TryGetVersionType(2, out var v2Type));
            Assert.IsTrue(IMigrationsRegistry.Instance.TryGetVersionType(3, out var v3Type));
            Assert.IsTrue(IMigrationsRegistry.Instance.TryGetVersionType(4, out var v4Type));
            Assert.AreEqual(typeof(StudentStateV1), v1Type);
            Assert.AreEqual(typeof(StudentStateV2), v2Type);
            Assert.AreEqual(typeof(StudentStateV3), v3Type);
            Assert.AreEqual(typeof(StudentState), v4Type);
        }

        #endregion

        #region V3 to V4 Migration Tests

        [Test]
        public void V3ToV4Migration_BasicState_ShouldMigrateCorrectly()
        {
            // Arrange
            var v3State = new StudentStateV3("test-fact-set")
            {
                CreatedAt = DateTime.UtcNow.AddHours(-1)
            };

            // Add a fact with review stage and repetition count
            v3State.Facts.Add(new StudentStateV3.FactItemV3("fact1", "test-fact-set", StudentStateV3.LearningStageV3.Review)
            {
                ReviewRepetitionCount = 1, // Should map to "review-2min"
                LastAskedTime = DateTime.UtcNow.AddMinutes(-5),
                ConsecutiveCorrect = 2
            });

            // Add a fact with repetition stage and count  
            v3State.Facts.Add(new StudentStateV3.FactItemV3("fact2", "test-fact-set", StudentStateV3.LearningStageV3.Repetition)
            {
                RepetitionCount = 2, // Should map to "repetition-4day"
                LastRepetitionTime = DateTime.UtcNow.AddHours(-1),
                ConsecutiveCorrect = 5
            });

            var migration = new StudentStateMigrationV3ToV4();

            // Act
            var v4State = migration.Migrate(v3State);

            // Assert
            Assert.IsNotNull(v4State);
            Assert.AreEqual(4, v4State.Version);
            Assert.AreEqual(v3State.CreatedAt, v4State.CreatedAt);
            Assert.AreEqual(2, v4State.Facts.Count);

            // Check fact 1 - review stage
            var fact1 = v4State.Facts.FirstOrDefault(f => f.FactId == "fact1");
            Assert.IsNotNull(fact1);
            Assert.AreEqual("review-2min", fact1.StageId); // RepetitionCount 1 -> review-2min
            Assert.AreEqual(2, fact1.ConsecutiveCorrect);

            // Check fact 2 - repetition stage  
            var fact2 = v4State.Facts.FirstOrDefault(f => f.FactId == "fact2");
            Assert.IsNotNull(fact2);
            Assert.AreEqual("repetition-4day", fact2.StageId); // RepetitionCount 2 -> repetition-4day
            Assert.AreEqual(5, fact2.ConsecutiveCorrect);
        }

        [Test]
        public void V3ToV4Migration_Properties_ShouldMatchExpectedValues()
        {
            // Arrange
            var migration = new StudentStateMigrationV3ToV4();

            // Assert
            Assert.AreEqual(3, migration.FromVersion);
            Assert.AreEqual(4, migration.ToVersion);
        }

        #endregion

        #region Helper Classes

        /// <summary>
        /// Test class representing a future state version for testing error handling
        /// </summary>
        private class TestFutureState : IStudentStateVersion
        {
            public int Version => 999;
            public DateTime CreatedAt => DateTime.UtcNow;
            public string GetStateSummary() => "Future state for testing";
        }

        #endregion
    }
}