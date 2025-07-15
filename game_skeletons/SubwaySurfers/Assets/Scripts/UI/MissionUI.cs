using System.Collections;
using Cysharp.Threading.Tasks;
using SubwaySurfers;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

public class MissionUI : MonoBehaviour
{
    public RectTransform missionPlace;
    public AssetReference missionEntryPrefab;
    public AssetReference addMissionButtonPrefab;

    public async UniTask OpenAsync()
    {
        var playerData = await IPlayerDataProvider.Instance.GetAsync();
        gameObject.SetActive(true);

        foreach (Transform t in missionPlace)
            Addressables.ReleaseInstance(t.gameObject);

        for(int i = 0; i < 3; ++i)
        {
            if (playerData.missions.Count > i)
            {
                AsyncOperationHandle op = missionEntryPrefab.InstantiateAsync();
                await op;
                if (op.Result == null || !(op.Result is GameObject))
                {
                    Debug.LogWarning(string.Format("Unable to load mission entry {0}.", missionEntryPrefab.Asset.name));
                    return;
                }
                MissionEntry entry = (op.Result as GameObject).GetComponent<MissionEntry>();
                entry.transform.SetParent(missionPlace, false);
                entry.FillWithMission(playerData.missions[i], this);
            }
            else
            {
                AsyncOperationHandle op = addMissionButtonPrefab.InstantiateAsync();
                await op;
                if (op.Result == null || !(op.Result is GameObject))
                {
                    Debug.LogWarning(string.Format("Unable to load button {0}.", addMissionButtonPrefab.Asset.name));
                    return;
                }
                AdsForMission obj = (op.Result as GameObject)?.GetComponent<AdsForMission>();
                obj.missionUI = this;
                obj.transform.SetParent(missionPlace, false);
            }
        }
    }

    public void CallOpen()
    {
        gameObject.SetActive(true);
        OpenAsync().Forget();
    }

    public async UniTaskVoid ClaimAsync(MissionBase m)
    {
        await IPlayerDataProvider.Instance.ClaimMissionAsync(m); 
        OpenAsync().Forget();
    }

    public void Close()
    {
        gameObject.SetActive(false);
    }
}
