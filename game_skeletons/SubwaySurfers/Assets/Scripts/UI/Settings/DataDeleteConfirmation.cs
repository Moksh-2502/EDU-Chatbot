using Cysharp.Threading.Tasks;
using SubwaySurfers;
using UnityEngine;

public class DataDeleteConfirmation : MonoBehaviour
{
    protected LoadoutState m_LoadoutState;

    public void Open(LoadoutState owner)
    {
        gameObject.SetActive(true);
        m_LoadoutState = owner;
    }

    public void Close()
    {
        gameObject.SetActive(false);
    }

    public void Confirm()
    {
        IPlayerDataProvider.Instance.ResetAsync().ContinueWith(() =>
        {
            m_LoadoutState.Refresh();
            Close();
        }).Forget();
    }

    public void Deny()
    {
        Close();
    }
}
