using UnityEngine;

namespace SubwaySurfers.Runtime
{
    /// <summary>
    /// Tracks distance to upcoming obstacles for gameplay mechanics like shackle debuff
    /// </summary>
    public class ObstacleDistanceTracker : MonoBehaviour
    {
        [Header("BoxCast Settings")] [SerializeField]
        private float _maxDetectionDistance = 100f;

        [SerializeField] private LayerMask _obstacleLayerMask;
        [SerializeField] private int _maxHitsPerRay = 10;
        [SerializeField] private Vector3 _boxCastHalfExtents = new Vector3(0.5f, 1f, 0.5f);
        [SerializeField] private Color _gizmoColor = new Color(0, 1, 0, 0.4f);
        [SerializeField] private bool _showGizmos = true;

        // Pre-allocated array for boxcast hits to avoid garbage collection
        private RaycastHit[] _boxcastHits = null;
        private int _currentHitCount = 0;
        private Vector3 _lastBoxcastOrigin;
        private Vector3 _lastHalfExtents;
        private Quaternion _lastRotation;
        private bool _didBoxCast;

        private TrackManager _trackManager;
        private CharacterInputController _characterController;

        private void Awake()
        {
            _boxcastHits = new RaycastHit[_maxHitsPerRay];

            _trackManager = FindFirstObjectByType<TrackManager>(FindObjectsInactive.Include);
        }

        private void Start()
        {
            _characterController = FindFirstObjectByType<CharacterInputController>(FindObjectsInactive.Include);
            Debug.Log($"[ObstacleDistanceTracker] Start - CharacterController found: {_characterController != null}");
        }

        /// <summary>
        /// Detects obstacles ahead using box cast that matches player's collider
        /// </summary>
        private void DetectObstaclesWithBoxCast()
        {
            if (_trackManager == null || !_trackManager.isMoving || _characterController == null ||
                _characterController.characterCollider == null)
            {
                _currentHitCount = 0;
                _didBoxCast = false;
                return;
            }

            Transform characterTransform = _characterController.characterCollider.transform;

            // Get collider dimensions if available
            Vector3 halfExtents = _boxCastHalfExtents;
            
            // Use the character's collider if available
            BoxCollider playerCollider = _characterController.characterCollider.collider;
            if (playerCollider != null)
            {
                halfExtents = Vector3.Scale(playerCollider.size * 0.5f, playerCollider.transform.lossyScale);
            }

            // Calculate start position based on character position
            Vector3 boxcastOrigin = characterTransform.position;
            
            // Store data for visualization
            _lastBoxcastOrigin = boxcastOrigin;
            _lastHalfExtents = halfExtents;
            _lastRotation = characterTransform.rotation;
            _didBoxCast = true;
            
            // Perform boxcast using non-allocating method
            _currentHitCount = Physics.BoxCastNonAlloc(
                boxcastOrigin,
                halfExtents,
                characterTransform.forward,
                _boxcastHits,
                characterTransform.rotation,
                _maxDetectionDistance,
                _obstacleLayerMask
            );
        }

        /// <summary>
        /// Gets the distance to the next obstacle
        /// </summary>
        /// <returns>Distance in world units, or float.MaxValue if no obstacles ahead</returns>
        public float GetDistanceToNextObstacle()
        {
            var obstacle = GetNextObstacle();
            if (obstacle == null)
            {
                return float.MaxValue;
            }

            var distance = Mathf.Abs(obstacle.transform.position.z - _characterController.transform.position.z);
            return distance;
        }

        /// <summary>
        /// Gets the closest obstacle game object
        /// </summary>
        /// <returns>The obstacle GameObject, or null if none found</returns>
        public GameObject GetNextObstacle()
        {
            DetectObstaclesWithBoxCast();
            if (_currentHitCount == 0)
            {
                return null;
            }

            for (var i = 0; i < _currentHitCount && i < _boxcastHits.Length; i++)
            {
                if (_boxcastHits[i].transform != null)
                {
                    return _boxcastHits[i].transform.gameObject;
                }
            }

            return null;
        }
        
        /// <summary>
        /// Visualizes the box cast in the scene view
        /// </summary>
        private void OnDrawGizmos()
        {
            if (!_showGizmos)
                return;
                
            // During gameplay, we use the last actual boxcast data
            if (Application.isPlaying && _didBoxCast)
            {
                DrawBoxCastGizmo(_lastBoxcastOrigin, _lastHalfExtents, _lastRotation, _maxDetectionDistance);
                return;
            }
            
            // In editor mode, try to find character controller to draw approximate boxcast
            if ((_characterController == null || _characterController.characterCollider == null) && !Application.isPlaying)
            {
                _characterController = FindFirstObjectByType<CharacterInputController>(FindObjectsInactive.Include);
                if (_characterController == null || _characterController.characterCollider == null)
                    return;
            }
            
            if (_characterController != null && _characterController.characterCollider != null)
            {
                Transform characterTransform = _characterController.characterCollider.transform;
                Vector3 halfExtents = _boxCastHalfExtents;
                
                BoxCollider playerCollider = _characterController.characterCollider.collider;
                if (playerCollider != null)
                {
                    halfExtents = Vector3.Scale(playerCollider.size * 0.5f, playerCollider.transform.lossyScale);
                }
                
                DrawBoxCastGizmo(characterTransform.position, halfExtents, characterTransform.rotation, _maxDetectionDistance);
            }
        }
        
        /// <summary>
        /// Draws box cast visualization using gizmos
        /// </summary>
        private void DrawBoxCastGizmo(Vector3 origin, Vector3 halfExtents, Quaternion rotation, float distance)
        {
            Gizmos.color = _gizmoColor;
            
            // Draw the origin box
            Matrix4x4 originalMatrix = Gizmos.matrix;
            Gizmos.matrix = Matrix4x4.TRS(origin, rotation, Vector3.one);
            Gizmos.DrawCube(Vector3.zero, halfExtents * 2);
            
            // Draw the direction and end box
            Vector3 direction = rotation * Vector3.forward;
            Vector3 endPosition = origin + direction * distance;
            Gizmos.matrix = Matrix4x4.TRS(endPosition, rotation, Vector3.one);
            Gizmos.DrawCube(Vector3.zero, halfExtents * 2);
            
            // Reset matrix and draw a line connecting the boxes
            Gizmos.matrix = originalMatrix;
            Gizmos.DrawLine(origin + rotation * new Vector3(halfExtents.x, 0, 0), 
                            endPosition + rotation * new Vector3(halfExtents.x, 0, 0));
            Gizmos.DrawLine(origin + rotation * new Vector3(-halfExtents.x, 0, 0), 
                            endPosition + rotation * new Vector3(-halfExtents.x, 0, 0));
            Gizmos.DrawLine(origin + rotation * new Vector3(0, halfExtents.y, 0), 
                            endPosition + rotation * new Vector3(0, halfExtents.y, 0));
            Gizmos.DrawLine(origin + rotation * new Vector3(0, -halfExtents.y, 0), 
                            endPosition + rotation * new Vector3(0, -halfExtents.y, 0));
            
            // Draw hit points if available
            if (Application.isPlaying && _currentHitCount > 0)
            {
                Gizmos.color = Color.red;
                for (int i = 0; i < _currentHitCount && i < _boxcastHits.Length; i++)
                {
                    if (_boxcastHits[i].transform != null)
                    {
                        Gizmos.DrawSphere(_boxcastHits[i].point, 0.2f);
                    }
                }
            }
        }
    }
}