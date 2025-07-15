using SubwaySurfers.LeaderboardSystem;
using UnityEngine;
using UnityEngine.UI;

namespace SubwaySurfers.Scripts.UI
{
    public class OpenLeaderboardButton : MonoBehaviour
    {
        [SerializeField] private Button button;

        private void Awake()
        {
            if (button == null)
            {
                button = this.GetComponentInChildren<Button>();
            }
            button.onClick.AddListener(OnClick);
        }

        private void OnClick()
        {
            LeaderboardsEventBus.RaiseLeaderboardEvent(new LeaderboardOpenEventArgs(LeaderboardSystemConstants.LeaderboardId, true));
        }
    }
}