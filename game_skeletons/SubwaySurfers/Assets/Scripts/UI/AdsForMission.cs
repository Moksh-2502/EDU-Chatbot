﻿using System;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using SubwaySurfers;
#if UNITY_ADS
using UnityEngine.Advertisements;
#endif

#if UNITY_ANALYTICS
using UnityEngine.Analytics;
#endif

public class AdsForMission : MonoBehaviour
{
    public MissionUI missionUI;

    public Text newMissionText;
    public Button adsButton;
#if UNITY_ANALYTICS
    public AdvertisingNetwork adsNetwork = AdvertisingNetwork.UnityAds;
#endif
    public string adsPlacementId = "rewardedVideo";
    public bool adsRewarded = true;

    void OnEnable()
    {
        RefreshStateAsync().Forget();
    }

    private async UniTaskVoid RefreshStateAsync()
    {
        try
        {
            adsButton.gameObject.SetActive(false);
            newMissionText.gameObject.SetActive(false);

            var playerData = await IPlayerDataProvider.Instance.GetAsync();

            // Only present an ad offer if less than 3 missions.
            if (playerData.missions.Count >= 3)
            {
                return;
            }

#if UNITY_ADS
        var isReady = Advertisement.IsReady(adsPlacementId);
        if (isReady)
        {
#if UNITY_ANALYTICS
            AnalyticsEvent.AdOffer(adsRewarded, adsNetwork, adsPlacementId, new Dictionary<string, object>
            {
                { "level_index", PlayerData.instance.rank },
                { "distance", TrackManager.instance == null ? 0 : TrackManager.instance.worldDistance },
            });
#endif
        }

        newMissionText.gameObject.SetActive(isReady);
        adsButton.gameObject.SetActive(isReady);
#endif
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
        }
    }

    public void ShowAds()
    {
#if UNITY_ADS
        if (Advertisement.IsReady(adsPlacementId))
        {
#if UNITY_ANALYTICS
            AnalyticsEvent.AdStart(adsRewarded, adsNetwork, adsPlacementId, new Dictionary<string, object>
            {
                { "level_index", PlayerData.instance.rank },
                { "distance", TrackManager.instance == null ? 0 : TrackManager.instance.worldDistance },
            });
#endif
            var options = new ShowOptions {resultCallback = HandleShowResult};
            Advertisement.Show(adsPlacementId, options);
        }
        else
        {
#if UNITY_ANALYTICS
            AnalyticsEvent.AdSkip(adsRewarded, adsNetwork, adsPlacementId, new Dictionary<string, object> {
                { "error", Advertisement.GetPlacementState(adsPlacementId).ToString() }
            });
#endif
        }
#endif
    }

#if UNITY_ADS
    private void HandleShowResult(ShowResult result)
    {
        switch (result)
        {
            case ShowResult.Finished:
                AddNewMission();
#if UNITY_ANALYTICS
                AnalyticsEvent.AdComplete(adsRewarded, adsNetwork, adsPlacementId);
#endif
                break;
            case ShowResult.Skipped:
                Debug.Log("The ad was skipped before reaching the end.");
#if UNITY_ANALYTICS
                AnalyticsEvent.AdSkip(adsRewarded, adsNetwork, adsPlacementId);
#endif
                break;
            case ShowResult.Failed:
                Debug.LogError("The ad failed to be shown.");
#if UNITY_ANALYTICS
                AnalyticsEvent.AdSkip(adsRewarded, adsNetwork, adsPlacementId, new Dictionary<string, object> {
                    { "error", "failed" }
                });
#endif
                break;
        }
    }
#endif

    private async UniTaskVoid AddNewMissionAsync()
    {
        await IPlayerDataProvider.Instance.AddMissionAsync();
        await missionUI.OpenAsync();
    }

    private void AddNewMission() => AddNewMissionAsync().Forget();
}