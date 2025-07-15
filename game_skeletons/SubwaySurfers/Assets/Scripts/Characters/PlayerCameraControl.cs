using System;
using Characters;
using UnityEngine;
using SubwaySurfers;
using Unity.Cinemachine;

namespace SubwaySurfers.Assets.Scripts.Characters
{
    /// <summary>
    /// Manages camera transitions based on player animation states and game states using Cinemachine.
    /// Listens to game state changes and animation state changes to switch between appropriate cameras.
    /// </summary>
    public class PlayerCameraControl : MonoBehaviour
    {
        [Header("Camera References")]
        [SerializeField] private CinemachineCamera loadoutCamera;
        [SerializeField] private CinemachineCamera normalCamera;
        [SerializeField] private CinemachineCamera hitCamera;
        [SerializeField] private CinemachineCamera deathCamera;
    
        
        // Animation state hashes for performance
        private static readonly int HitHash = Animator.StringToHash("Hit");
        private static readonly int DeadHash = Animator.StringToHash("Dead");
        
        // Component references
        private IGameManager gameManager;
        private CharacterCollider characterCollider;

        private bool _isInitialized = false;

        private CinemachineCamera[] _cameras;
        

        #region Unity Lifecycle

        private void Awake()
        {
            InitializeReferences();
            SwitchToCamera(loadoutCamera);
        }

        private void OnEnable()
        {
            InitializeReferences();
            SubscribeToEvents();
        }

        private void OnDisable()
        {
            UnsubscribeFromEvents();
        }

        #endregion

        private void OnCharacterSMBChanged(CharacterSMBEventArgs args)
        {
            switch (args.StateType)
            {
                case AnimatorStateType.Normal:
                    SwitchToCamera(normalCamera);
                    break;
                case AnimatorStateType.Hit:
                    SwitchToCamera(hitCamera);
                    break;
                case AnimatorStateType.Death:
                    SwitchToCamera(deathCamera);
                    break;
                default:
                    Debug.LogWarning($"[PlayerCameraControl] Unhandled state type: {args.StateType}", this);
                    break;
            }
        }

        #region Initialization

        private void InitializeReferences()
        {
            if (_isInitialized)
            {
                return;
            }
            _isInitialized = true;
            try
            {
                // Find game manager
                gameManager = FindFirstObjectByType<GameManager>(FindObjectsInactive.Include);
                if (gameManager == null)
                {
                    Debug.LogError("[PlayerCameraControl] GameManager not found!", this);
                    return;
                }

                // Find character controller
                characterCollider = FindFirstObjectByType<CharacterCollider>(FindObjectsInactive.Include);
                if (characterCollider == null)
                {
                    Debug.LogError("[PlayerCameraControl] characterCollider not found!", this);
                    return;
                }
                
                if (normalCamera != null)
                {
                    normalCamera.Target = new CameraTarget() {TrackingTarget =  characterCollider.transform};
                }
                if (hitCamera != null)
                {
                    hitCamera.Target = new CameraTarget() {TrackingTarget = characterCollider.transform};
                }
                
                if (deathCamera != null)
                {
                    deathCamera.Target = new CameraTarget() {TrackingTarget = characterCollider.transform};
                }
                
                _cameras = new CinemachineCamera[]
                {
                    loadoutCamera,
                    normalCamera,
                    hitCamera,
                    deathCamera
                };
                
                Debug.Log("[PlayerCameraControl] Successfully initialized");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[PlayerCameraControl] Initialization failed: {ex.Message}", this);
            }
        }

        #endregion

        #region Event Management

        private void SubscribeToEvents()
        {
            CharacterAnimatorSMBListener.OnSMBChanged += OnCharacterSMBChanged;
            if (gameManager != null)
            {
                gameManager.OnGameStateChanged += OnGameStateChanged;
            }
        }

        private void UnsubscribeFromEvents()
        {
            CharacterAnimatorSMBListener.OnSMBChanged -= OnCharacterSMBChanged;
            if (gameManager != null)
            {
                gameManager.OnGameStateChanged -= OnGameStateChanged;
            }
        }

        #endregion

        #region Event Handlers

        private void OnGameStateChanged(AState newState)
        {
            // Reset to normal camera when entering GameState
            if (newState is GameState)
            {
                SwitchToCamera(normalCamera);
                Debug.Log("[PlayerCameraControl] Game state entered - switching to normal camera");
            }
            else if (newState is LoadoutState)
            {
                SwitchToCamera(loadoutCamera);
            }
        }

        #endregion



        #region Camera Control
        private void SwitchToCamera(CinemachineCamera targetCamera)
        {
            if (targetCamera == null)
            {
                Debug.LogWarning("[PlayerCameraControl] Target camera is null!", this);
                return;
            }

            foreach (var cam in _cameras)
            {
                if (cam == null)
                {
                    continue;
                }
                cam.Priority = cam == targetCamera ? 100 : 0;
            }

            Debug.Log($"[PlayerCameraControl] Switched to camera: {targetCamera.name}");
        }

        #endregion
    }
} 