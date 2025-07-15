using System.Collections;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using SubwaySurfers;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
#if UNITY_ANALYTICS
using UnityEngine.Analytics;
#endif
#if UNITY_PURCHASING
using UnityEngine.Purchasing;
#endif

public class StartButton : MonoBehaviour
{
    [SerializeField] private Selectable _button;
    [SerializeField] private TMP_Text _buttonText;
    private async UniTaskVoid StartGameAsync()
    {
        if (_button != null)
        {
            _button.interactable = false;
        }
        
        if (_buttonText != null)
        {
            _buttonText.text = "Loading...";
        }
        var playerData = await IPlayerDataProvider.Instance.GetAsync();
        if (playerData.ftueLevel == 0)
        {
            await IPlayerDataProvider.Instance.SetFTUELevelAsync(1);
        }
#if UNITY_ANALYTICS
            AnalyticsEvent.FirstInteraction("start_button_pressed");
#endif
#if UNITY_PURCHASING
        var module = StandardPurchasingModule.Instance();
#endif
        SceneManager.LoadScene("main");
    }
    public void StartGame()
    {
        StartGameAsync().Forget();
    }
}
