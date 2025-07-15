using UnityEngine;
using AIEduChatbot.UnityReactBridge.Handlers;
using Cysharp.Threading.Tasks;
using Sounds;
using SubwaySurfers;

/// <summary>
/// state pushed on top of the GameManager when the player dies.
/// </summary>
public class GameOverState : AState
{
    public TrackManager trackManager;
    public Canvas canvas;
    public MissionUI missionPopup;

	public AudioClip gameOverTheme;

    public override void Enter(AState from)
    {
        canvas.gameObject.SetActive(true);

        missionPopup.gameObject.SetActive(false);

		CreditCoins();

		if (MusicPlayer.instance.GetStem(0) != gameOverTheme)
		{
            MusicPlayer.instance.SetStem(0, gameOverTheme);
			StartCoroutine(MusicPlayer.instance.RestartAllStems());
        }
    }

	public override void Exit(AState to)
    {
        canvas.gameObject.SetActive(false);
        FinishRun();
    }

    public override string GetName()
    {
        return "GameOver";
    }

    public override void Tick()
    {
        
    }

	public void GoToStore()
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene("shop", UnityEngine.SceneManagement.LoadSceneMode.Additive);
    }


    public void GoToLoadout()
    {
        // Clean up current run and ensure proper state reset
        if (trackManager != null)
        {
            trackManager.isRerun = false;
        }
        
        manager.SwitchState("Loadout");
    }

    public void RunAgain()
    {
	    // Clean up current run and ensure proper state reset
	    if (trackManager != null)
	    {
		    trackManager.isRerun = false;
	    }
	    manager.SwitchState("Game");
    }

    protected void CreditCoins()
    {
	    IPlayerDataProvider.Instance.SaveAsync().Forget();
	}

	protected void FinishRun()
    {
        trackManager.End();
    }

    //----------------
}
