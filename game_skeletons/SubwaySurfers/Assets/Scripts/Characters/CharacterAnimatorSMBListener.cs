using UnityEngine;

namespace Characters
{
    public struct CharacterSMBEventArgs
    {
        public AnimatorStateType StateType { get; }

        public CharacterSMBEventArgs(AnimatorStateType stateType)
        {
            StateType = stateType;
        }
    }

    public enum AnimatorStateType
    {
        Normal,
        Death,
        Hit
    }
    public delegate void CharacterSMBEventHandler(CharacterSMBEventArgs args);
    public static class CharacterAnimatorSMBListener
    {
        public static event CharacterSMBEventHandler OnSMBChanged;
        
        public static void OnStateEnter(AnimatorStateType stateType)
        {
            OnSMBChanged?.Invoke(new CharacterSMBEventArgs(stateType));
        }
    }
}