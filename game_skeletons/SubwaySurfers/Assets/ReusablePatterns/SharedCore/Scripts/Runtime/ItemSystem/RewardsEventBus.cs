using System;
using System.Collections.Generic;

namespace ReusablePatterns.SharedCore.Scripts.Runtime.ItemSystem
{
    public struct SelectRewardClaimRequestEventArgs
    {
        public string RewardsId { get; private set; }
        public string Title { get; private set; }

        public SelectRewardClaimRequestEventArgs(string title, string rewardsId)
        {
            RewardsId = rewardsId;
            this.Title = title;
        }
    }
    public struct RewardClaimResultEventArgs
    {
        public string RewardsId { get; private set; }
        public ClaimRewardResult Result { get; private set; }
        public RewardClaimResultEventArgs(string rewardsId, ClaimRewardResult result)
        {
            RewardsId = rewardsId;
            Result = result;
        }
    }

    public struct ClaimableRewardsStatusEventArgs
    {
        public bool HasClaimableRewards { get; private set; }
        public int ClaimableRewardsCount { get; private set; }
        public IList<string> ClaimableRewardsIds { get; private set; }

        public ClaimableRewardsStatusEventArgs(bool hasClaimableRewards, int claimableRewardsCount, IList<string> claimableRewardsIds)
        {
            HasClaimableRewards = hasClaimableRewards;
            ClaimableRewardsCount = claimableRewardsCount;
            ClaimableRewardsIds = claimableRewardsIds;
        }
    }
    public static class RewardsEventBus
    {
        public static event Action<SelectRewardClaimRequestEventArgs> OnSelectRewardClaimRequest;
        public static event Action<RewardClaimResultEventArgs> OnRewardClaimResult;
        public static event Action<ClaimableRewardsStatusEventArgs> OnClaimableRewardsStatusChanged;

        public static void RaiseSelectRewardClaimRequest(string title, string rewardsId)
        {
            OnSelectRewardClaimRequest?.Invoke(new SelectRewardClaimRequestEventArgs(title, rewardsId));
        }
        public static void RaiseRewardClaimResult(string rewardsId, ClaimRewardResult result)
        {
            OnRewardClaimResult?.Invoke(new RewardClaimResultEventArgs(rewardsId, result));
        }
        public static void RaiseClaimableRewardsStatusChanged(bool hasClaimableRewards, int claimableRewardsCount, IList<string> claimableRewardsIds)
        {
            OnClaimableRewardsStatusChanged?.Invoke(new ClaimableRewardsStatusEventArgs(hasClaimableRewards, claimableRewardsCount, claimableRewardsIds));
        }
    }
}