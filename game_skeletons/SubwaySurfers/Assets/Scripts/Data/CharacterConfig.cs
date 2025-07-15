using UnityEngine;

[CreateAssetMenu(fileName = "CharacterConfig", menuName = "Trash Dash/CharacterConfig")]
public class CharacterConfig : ScriptableObject
{
    [field: SerializeField] public int StartingLives { get; private set; }
    [field: SerializeField] public int MaxLives { get; private set; }
    [field: SerializeField] public bool ResetSpeedOnHit { get; private set; }
}
