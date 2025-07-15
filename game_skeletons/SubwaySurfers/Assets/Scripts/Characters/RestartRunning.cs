using UnityEngine;

public class RestartRunning : StateMachineBehaviour
{
	static int s_DeadHash = Animator.StringToHash("Dead");

    private CharacterInputController _characterInputController;

    override public void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        // We don't restart if we go toward the death state
        if (animator.GetBool(s_DeadHash))
            return;

        bool isRestart = true;
        if (_characterInputController == null)
        {
            _characterInputController = animator.GetComponentInParent<CharacterInputController>(true);
        }

        if (_characterInputController != null && _characterInputController.CharacterConfig != null)
        {
            isRestart = _characterInputController.CharacterConfig.ResetSpeedOnHit;
        }
        
        TrackManager.instance.StartMove(isRestart);
    }

}
