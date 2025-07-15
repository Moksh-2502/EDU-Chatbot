using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Pool;
using SubwaySurfers.LeaderboardSystem;
using DG.Tweening;
using TMPro;

namespace SubwaySurfers.Scripts.UI
{
    public class LeaderboardEntriesDrawer : MonoBehaviour
    {
        [Header("UI References")] [SerializeField]
        private GameObject[] animatables;

        [SerializeField] private TMP_Text titleText;
        [SerializeField] private Transform entriesContainer;
        [SerializeField] private LeaderboardEntryUIItem entryPrefab;
        [SerializeField] private GameObject loadingIndicator;
        [SerializeField] private GameObject errorPanel;

        [Header("Settings")] [SerializeField] private int maxEntriesToShow = 10;
        [SerializeField] private float animationDuration = 0.3f;
        [SerializeField] private Ease showEase = Ease.OutBack;
        [SerializeField] private Ease hideEase = Ease.InBack;

        private ObjectPool<LeaderboardEntryUIItem> _entryPool;
        private readonly List<LeaderboardEntryUIItem> _activeEntries = new();

        private CancellationTokenSource _repaintCts = null;

        private void Awake()
        {
            InitializeObjectPool();

            // Initialize root scale to 0 so it's hidden by default
            if (animatables != null)
            {
                foreach (var item in animatables)
                {
                    if (item != null)
                    {
                        item.transform.localScale = Vector3.zero;
                        item.SetActive(false);
                    }
                }
            }
        }

        private void OnEnable()
        {
            LeaderboardsEventBus.OnLeaderboardEvent += OnLeaderboardEvent;
        }

        private void OnDisable()
        {
            LeaderboardsEventBus.OnLeaderboardEvent -= OnLeaderboardEvent;
            _repaintCts?.Cancel();
            _repaintCts = null;
        }

        private void OnLeaderboardEvent(LeaderboardEventArgs args)
        {
            if (args is LeaderboardOpenEventArgs openArgs)
            {
                if (openArgs.Show)
                {
                    ShowWindow();
                }
                else
                {
                    HideWindow();
                }
            }
        }

        private void ShowAnimatable(GameObject item)
        {
            if (item == null) return;

            // Activate the root GameObject
            item.SetActive(true);

            // Animate scale from 0 to 1
            item.transform
                .DOScale(Vector3.one, animationDuration)
                .SetEase(showEase);
        }

        private void ShowWindow()
        {
            // Set title to current week of the year
            if (titleText != null)
            {
                int weekOfYear = ISOWeek.GetWeekOfYear(DateTime.Now);
                titleText.text = $"Week {weekOfYear}";
            }

            foreach (var item in animatables)
            {
                ShowAnimatable(item);
            }

            _repaintCts?.Cancel();
            _repaintCts = new CancellationTokenSource();
            RefreshLeaderboard(_repaintCts.Token).Forget();
        }

        private void HideAnimatable(GameObject item)
        {
            if (item == null) return;

            // Animate scale from current to 0
            item.transform
                .DOScale(Vector3.zero, animationDuration)
                .SetEase(hideEase)
                .OnComplete(() =>
                {
                    // Deactivate the root GameObject when animation completes
                    item.SetActive(false);
                });
        }

        private void HideWindow()
        {
            foreach (var item in animatables)
            {
                HideAnimatable(item);
            }
        }

        public void Close()
        {
            // Public method that can be called from UI buttons or other systems
            LeaderboardsEventBus.RaiseLeaderboardEvent(
                new LeaderboardOpenEventArgs(LeaderboardSystemConstants.LeaderboardId, false));
        }

        private void InitializeObjectPool()
        {
            _entryPool = new ObjectPool<LeaderboardEntryUIItem>(
                createFunc: () => CreateEntry(),
                actionOnGet: (entry) => OnGetEntry(entry),
                actionOnRelease: (entry) => OnReleaseEntry(entry),
                actionOnDestroy: (entry) => OnDestroyEntry(entry)
            );
        }

        private LeaderboardEntryUIItem CreateEntry()
        {
            var entry = Instantiate(entryPrefab, entriesContainer);
            entry.gameObject.SetActive(false);
            return entry;
        }

        private void OnGetEntry(LeaderboardEntryUIItem entry)
        {
            entry.transform.SetParent(entriesContainer);
            entry.gameObject.SetActive(true);
        }

        private void OnReleaseEntry(LeaderboardEntryUIItem entry)
        {
            entry.gameObject.SetActive(false);
        }

        private void OnDestroyEntry(LeaderboardEntryUIItem entry)
        {
            if (entry != null && entry.gameObject != null)
            {
                Destroy(entry.gameObject);
            }
        }

        private async UniTaskVoid RefreshLeaderboard(CancellationToken cancellationToken)
        {
            SetLoadingState(true);
            SetErrorState(false);

            try
            {
                // Fetch leaderboard data
                var entries = await ILeaderboardService.Instance.GetTopEntriesWithLocalPlayerAsync(maxEntriesToShow,
                    LeaderboardSystemConstants.LeaderboardId, cancellationToken);
                cancellationToken.ThrowIfCancellationRequested();
                UpdateLeaderboardUI(entries);
            }
            catch (OperationCanceledException)
            {
                
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Failed to refresh leaderboard: {ex.Message}");
                SetErrorState(true);
            }
            finally
            {
                SetLoadingState(false);
            }
        }

        private void UpdateLeaderboardUI(List<LeaderboardEntryWithLocalFlag> entries)
        {
            // Release all current entries back to pool
            ReturnAllEntriesToPool();

            // Create UI elements for new entries
            for (int i = 0; i < entries.Count; i++)
            {
                var entryUI = _entryPool.Get();
                entryUI.SetData(entries[i]);
                entryUI.transform.SetSiblingIndex(i);
                _activeEntries.Add(entryUI);
            }

            Debug.Log($"Updated leaderboard UI with {entries.Count} entries");
        }

        private void ReturnAllEntriesToPool()
        {
            foreach (var entry in _activeEntries)
            {
                _entryPool.Release(entry);
            }

            _activeEntries.Clear();
        }

        private void SetLoadingState(bool isLoading)
        {
            if (loadingIndicator != null)
                loadingIndicator.SetActive(isLoading);
        }

        private void SetErrorState(bool hasError)
        {
            if (errorPanel != null)
                errorPanel.SetActive(hasError);
        }

        private void OnDestroy()
        {
            // Clean up object pool
            ReturnAllEntriesToPool();
            _entryPool?.Dispose();
        }
    }
}