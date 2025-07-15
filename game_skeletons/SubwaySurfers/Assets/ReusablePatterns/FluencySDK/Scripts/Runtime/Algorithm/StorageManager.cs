using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using AIEduChatbot.UnityReactBridge.Storage;
using FluencySDK.Migrations;
using UnityEngine;

namespace FluencySDK.Algorithm
{
    public class StorageManager
    {
        private const string k_FluencyStateStorageKey = "FluencyState";

        public StudentState StudentState { get; private set; }
        public Dictionary<string, FactSet> FactSetsById { get; private set; }

        private readonly LearningAlgorithmConfig _config;
        private readonly IGameStorageService _gameStorageService;

        public StorageManager(LearningAlgorithmConfig config, IGameStorageService gameStorageService)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _gameStorageService = gameStorageService ?? throw new ArgumentNullException(nameof(gameStorageService));
        }

        public async UniTask Initialize()
        {
            if (_config.AlwaysStartFresh)
            {
                StudentState = IStudentStateMigrationService.Instance.CreateNewState();
            }
            else
            {
                StudentState = await LoadOrCreateStudentState();
            }

            FactSetsById = BuildFactSets();
            LoadAllFactsUpfront();
            RemoveAllMissingFacts();
            await SaveStateAsync();
        }

        private void RemoveAllMissingFacts()
        {
            var toRemove = StudentState.Facts
                .Where(fact => GetFactById(fact.FactId) == null)
                .ToList();

            foreach (var fact in toRemove)
            {
                StudentState.Facts.Remove(fact);
            }
        }

        public Fact GetFactById(string factId)
        {
            foreach (var factSet in FactSetsById.Values)
            {
                var fact = factSet.Facts.FirstOrDefault(f => f.Id == factId);
                if (fact != null) return fact;
            }
            return null;
        }

        private async UniTask<StudentState> LoadOrCreateStudentState()
        {
            try
            {
                var exists = await _gameStorageService.ExistsAsync(k_FluencyStateStorageKey);
                if (exists)
                {
                    return await _gameStorageService.LoadAsync<StudentState>(k_FluencyStateStorageKey);
                }
                else
                {
                    return IStudentStateMigrationService.Instance.CreateNewState();
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error loading student state: {ex.Message}");
                return IStudentStateMigrationService.Instance.CreateNewState();
            }
        }

        public async UniTask SaveStateAsync()
        {
            try
            {
                await _gameStorageService.SetAsync(k_FluencyStateStorageKey, StudentState);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error saving student state: {ex.Message}");
            }
        }

        private Dictionary<string, FactSet> BuildFactSets()
        {
            var factSetBuilder = new FactSetBuilder(_config);
            var factSetsById = factSetBuilder.BuildAllFactSets();
            Debug.Log($"[StorageManager] Built {factSetsById.Count} fact sets");
            return factSetsById;
        }

        private void LoadAllFactsUpfront()
        {
            foreach (var factSetId in _config.FactSetOrder)
            {
                LoadFactSet(factSetId);
            }
            Debug.Log($"[StorageManager] Loaded all fact sets upfront, total facts: {StudentState.Facts.Count}");
        }

        private void LoadFactSet(string factSetId)
        {
            if (!FactSetsById.TryGetValue(factSetId, out FactSet factSet))
            {
                Debug.LogError($"Fact set {factSetId} not found!");
                return;
            }

            var existingFacts = StudentState.GetFactsForSet(factSetId);
            var existingFactIds = existingFacts.Select(f => f.FactId).ToHashSet();
            var factSetFactIds = factSet.Facts.Select(f => f.Id).ToList();
            var missingFactIds = factSetFactIds.Where(id => !existingFactIds.Contains(id)).ToList();

            if (missingFactIds.Count == 0) return;

            SharpShuffleBag.Shuffle.FisherYates(missingFactIds);

            foreach (var factId in missingFactIds)
            {
                var firstStage = _config.GetFirstStage();
                StudentState.Facts.Add(new FactItem(factId, factSetId, firstStage.Id));
            }

            Debug.Log($"[StorageManager] Added {missingFactIds.Count} new facts from fact set {factSetId}");
        }
    }
} 