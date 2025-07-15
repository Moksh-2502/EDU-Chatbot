using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using ReusablePatterns.FluencySDK.Scripts.Runtime.LearningProgress.Models;
using UnityEngine;
using UnityEngine.UI;
using FluencySDK;
using ReusablePatterns.SharedCore.Runtime.PauseSystem;

namespace ReusablePatterns.FluencySDK.Scripts.Runtime.LearningProgress.UI
{
    /// <summary>
    /// Simple fact set progress display matching the 4-column layout:
    /// Fact Set Name | Learning Stage + Icon | Progress | Claim Button
    /// </summary>
    public class FactSetProgressUI : FactSetListViewBase<FactSetRowItem, FactSetUIItemInfo>
    {
        [System.Serializable]
        private class StageIcon
        {
            [field: SerializeField] public LearningStageType TargetStage { get; private set; }
            [field: SerializeField] public Sprite Icon { get; private set; }
        }

        [Header("Animation & UI")] [SerializeField]
        private Button refreshButton;

        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private Transform rootTransform;
        [SerializeField] private Button closeButton;

        [Header("Stage Icons")] [SerializeField]
        private StageIcon[] stageIcons;


        protected override void OnInitialize()
        {
            base.OnInitialize();
            if (refreshButton != null)
                refreshButton.onClick.AddListener(() =>
                    RefreshData(ILearningProgressService.Instance?.GetFactSetProgresses()));
            if (closeButton != null)
            {
                closeButton.onClick.AddListener(Hide);
            }
        }

        /// <summary>
        /// Transforms FactSetProgress data into FactSetUIItemInfo for display
        /// </summary>
        protected override IReadOnlyList<FactSetUIItemInfo> TransformData(IList<FactSetProgress> source)
        {
            var itemInfoList = new List<FactSetUIItemInfo>();

            foreach (var factSetProgress in source)
            {
                var dominantStage = factSetProgress.GetDominantStage();
                var icon = GetStageIcon(dominantStage);
                var itemInfo = new FactSetUIItemInfo(factSetProgress, icon);
                itemInfoList.Add(itemInfo);
            }

            return itemInfoList;
        }

        public void Show()
        {
            Initialize();
            if (IGamePauser.Instance != null)
            {
                IGamePauser.Instance.Pause(new PauseData()
                {
                    displayMenu = false,
                    resumeWithCountdown = true,
                });
            }

            // Activate the game object first
            gameObject.SetActive(true);

            // Set initial states
            if (rootTransform != null)
                rootTransform.localScale = Vector3.zero;
            if (canvasGroup != null)
                canvasGroup.alpha = 0f;

            // Animate scale and fade in
            if (rootTransform != null)
            {
                rootTransform.DOScale(Vector3.one, 0.3f).SetEase(Ease.OutBack)
                    .SetUpdate(true);
            }

            if (canvasGroup != null)
            {
                canvasGroup.DOFade(1f, 0.3f).SetEase(Ease.OutQuad).SetUpdate(true).OnComplete(() =>
                    RefreshData(ILearningProgressService.Instance?.GetFactSetProgresses()));
            }
            else
            {
                RefreshData(ILearningProgressService.Instance?.GetFactSetProgresses());
            }
        }

        private void Hide()
        {
            // Create a sequence to handle both animations
            var sequence = DOTween.Sequence();
            if (IGamePauser.Instance != null)
            {
                IGamePauser.Instance.Resume();
            }

            sequence.SetUpdate(true);
            if (rootTransform != null)
            {
                sequence.Join(rootTransform.DOScale(Vector3.zero, 0.25f).SetEase(Ease.InBack));
            }

            if (canvasGroup != null)
            {
                sequence.Join(canvasGroup.DOFade(0f, 0.25f).SetEase(Ease.InQuad));
            }

            sequence.OnComplete(() => gameObject.SetActive(false));
        }


        private Sprite GetStageIcon(LearningStage stage)
        {
            return stageIcons.FirstOrDefault(o => o.TargetStage == stage.Type)?.Icon;
        }
    }
}