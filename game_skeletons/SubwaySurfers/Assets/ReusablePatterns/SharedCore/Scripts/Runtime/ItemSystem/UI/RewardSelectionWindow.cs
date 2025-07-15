using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.UI;
using DG.Tweening;

namespace ReusablePatterns.SharedCore.Scripts.Runtime.ItemSystem.UI
{
    /// <summary>
    /// Simple popup window for displaying reward selection options
    /// </summary>
    public class RewardSelectionWindow : MonoBehaviour
    {
        [Header("UI Components")] [SerializeField]
        private RectTransform root;

        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private RewardsConfig rewardsConfig;
        [SerializeField] private Button closeButton;
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private Transform rewardCardsContainer;
        [SerializeField] private RewardCard rewardCardPrefab;
        [SerializeField] private GameObject noDataGO;
        private ObjectPool<RewardCard> _cardsPool = null;
        private readonly List<RewardCard> _spawnedCards = new();

        private string _rewardsId = null;

        private void OnEnable()
        {
            RewardsEventBus.OnSelectRewardClaimRequest += OnSelectRewardToClaimRequest;
            RewardsEventBus.OnRewardClaimResult += OnRewardClaimResult;
        }

        private void OnDisable()
        {
            RewardsEventBus.OnSelectRewardClaimRequest -= OnSelectRewardToClaimRequest;
            RewardsEventBus.OnRewardClaimResult -= OnRewardClaimResult;
        }

        private void Awake()
        {
            closeButton?.onClick.AddListener(Hide);
            _cardsPool = new ObjectPool<RewardCard>(
                () => Instantiate(rewardCardPrefab, rewardCardsContainer),
                fetched => fetched.gameObject.SetActive(true),
                released => released.gameObject.SetActive(false),
                destroyed => Destroy(destroyed.gameObject));
            Hide();
        }

        private void OnRewardClaimResult(RewardClaimResultEventArgs args)
        {
            if (args.RewardsId != this._rewardsId)
            {
                return;
            }

            if (args.Result.Status == RewardStatus.Claimed)
            {
                Hide();
            }
        }

        private void OnSelectRewardToClaimRequest(SelectRewardClaimRequestEventArgs args)
        {
            OpenRewardSelection(args.Title, args.RewardsId);
        }


        /// <summary>
        /// Opens the reward selection window for a specific fact set
        /// </summary>
        private void OpenRewardSelection(string title, string rewardsId)
        {
            if (rewardsConfig == null || rewardsConfig.TryGetRewardData(rewardsId, out var data) == false)
            {
                noDataGO.SetActive(true);
                return;
            }

            noDataGO.SetActive(false);

            _rewardsId = rewardsId;

            if (titleText != null)
            {
                titleText.text = title;
            }

            RepaintItems(data.Items);
            Show();
        }

        public void Show()
        {
            // Activate the game object first
            root.gameObject.SetActive(true);

            // Enable canvas group interactions
            if (canvasGroup != null)
            {
                canvasGroup.interactable = true;
                canvasGroup.blocksRaycasts = true;
            }

            // Create a sequence to handle both animations
            var sequence = DOTween.Sequence();
            sequence.SetUpdate(true);

            if (root != null)
            {
                sequence.Join(root.DOScale(Vector3.one, 0.3f).SetEase(Ease.OutBack));
            }

            if (canvasGroup != null)
            {
                sequence.Join(canvasGroup.DOFade(1f, 0.3f).SetEase(Ease.OutQuad));
            }
        }

        public void Hide()
        {
            // Create a sequence to handle both animations
            var sequence = DOTween.Sequence();
            sequence.SetUpdate(true);

            if (root != null)
            {
                sequence.Join(root.DOScale(Vector3.zero, 0.25f).SetEase(Ease.InBack));
            }

            if (canvasGroup != null)
            {
                sequence.Join(canvasGroup.DOFade(0f, 0.25f).SetEase(Ease.InQuad));
            }

            sequence.OnComplete(() =>
            {
                root.gameObject.SetActive(false);
                // Disable canvas group interactions
                if (canvasGroup != null)
                {
                    canvasGroup.interactable = false;
                    canvasGroup.blocksRaycasts = false;
                }
            });
        }

        private void RepaintItems(IList<ItemData> items)
        {
            if (items == null || rewardCardPrefab == null || rewardCardsContainer == null)
                return;

            ClearRewardCards();

            foreach (var itemData in items)
            {
                var cardObj = _cardsPool.Get();
                _spawnedCards.Add(cardObj);
                cardObj.Repaint((_rewardsId, itemData));
            }
        }

        private void ClearRewardCards()
        {
            foreach (var card in _spawnedCards)
            {
                _cardsPool.Release(card);
            }

            _spawnedCards.Clear();
        }
    }
}