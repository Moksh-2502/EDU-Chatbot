using UnityEngine;

[CreateAssetMenu(fileName = "GamePhysicsConfig", menuName = "Trash Dash/GamePhysicsConfig")]
public class GamePhysicsConfig : ScriptableObject
{
    [field: SerializeField] public LayerMask ObstacleLayer { get; private set; }
    [field: SerializeField] public float ObstacleCheckRadius { get; private set; }
}

