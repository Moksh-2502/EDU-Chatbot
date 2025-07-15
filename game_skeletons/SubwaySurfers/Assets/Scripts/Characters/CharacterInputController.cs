using System;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using Characters;
using Consumables;
using SubwaySurfers.Runtime;
using SubwaySurfers;
using SubwaySurfers.Assets.Scripts.Tracks;
using ReusablePatterns.SharedCore.Runtime.PauseSystem;

public delegate void LaneSwitchEventHandler(bool isLeft);

/// <summary>
/// Handle everything related to controlling the character. Interact with both the Character (visual, animation) and CharacterCollider
/// 
/// Character Responsiveness Notes:
/// - Animation speeds are clamped to minimum values to prevent slow-motion feel at low difficulty levels
/// - Jump and slide animations maintain visual responsiveness while preserving gameplay distance mechanics
/// - Character responsiveness multiplier provides additional animation speed boost for better game feel
/// </summary>
public class CharacterInputController : MonoBehaviour
{
    public event Action<bool> OnSwitchLaneAttempt;
    public event LaneSwitchEventHandler OnLaneSwitched;
    public event Action OnJumped, OnDucked;

    static int s_DeadHash = Animator.StringToHash("Dead");
    static int s_RunStartHash = Animator.StringToHash("runStart");
    static int s_MovingHash = Animator.StringToHash("Moving");
    static int s_JumpingHash = Animator.StringToHash("Jumping");
    static int s_JumpingSpeedHash = Animator.StringToHash("JumpSpeed");
    static int s_SlidingHash = Animator.StringToHash("Sliding");

    [field: SerializeField] public CharacterConfig CharacterConfig { get; private set; }

    public TrackManager trackManager;
    public Character character;
    public CharacterCollider characterCollider;
    public GameObject blobShadow;
    public float laneChangeSpeed = 1.0f;

    public Consumable inventory;

    public readonly List<Consumable> consumables = new List<Consumable>();

    public bool isJumping
    {
        get { return m_Jumping; }
    }

    public bool isSliding
    {
        get { return m_Sliding; }
    }

    [Header("Controls")] public float jumpLength = 2.0f; // Distance jumped
    public float jumpHeight = 1.2f;

    public float slideLength = 2.0f;

    [Header("Sounds")] public AudioClip slideSound;
    public AudioClip powerUpUseSound;
    public AudioSource powerupSource;


    protected int m_ObstacleLayer;

    protected bool m_IsInvincible;
    protected bool m_IsRunning;

    protected float m_JumpStart;
    protected bool m_Jumping;

    protected bool m_Sliding;
    protected float m_SlideStart;

    protected AudioSource m_Audio;

    protected int m_CurrentLane = k_StartingLane;
    protected Vector3 m_TargetPosition = Vector3.zero;

    protected readonly Vector3 k_StartingPosition = Vector3.forward * 2f;

    protected const int k_StartingLane = 1;
    protected const float k_GroundingSpeed = 80f;
    protected const float k_ShadowRaycastDistance = 100f;
    protected const float k_ShadowGroundOffset = 0.01f;

    private ILootablesSpawner _lootablesSpawner;
    private IGamePauser _gamePauser;

    protected void Awake()
    {
        _lootablesSpawner = FindFirstObjectByType<LootablesSpawner>(FindObjectsInactive.Include);
        _gamePauser = FindFirstObjectByType<PauseManager>(FindObjectsInactive.Include);
    }

    private void OnEnable()
    {
        GameInputController.OnLeftInput += OnLeftInput;
        GameInputController.OnRightInput += OnRightInput;
        GameInputController.OnUpInput += OnUpInput;
        GameInputController.OnJumpInput += OnUpInput;
        GameInputController.OnDownInput += OnDownInput;
    }

    private void OnDisable()
    {
        GameInputController.OnLeftInput -= OnLeftInput;
        GameInputController.OnRightInput -= OnRightInput;
        GameInputController.OnUpInput -= OnUpInput;
        GameInputController.OnJumpInput -= OnUpInput;
        GameInputController.OnDownInput -= OnDownInput;
    }

    // Cheating functions, use for testing
    public void CheatInvincible(bool invincible)
    {
        m_IsInvincible = invincible;
    }

    public bool IsCheatInvincible()
    {
        return m_IsInvincible;
    }

    public void Init()
    {
        transform.position = k_StartingPosition;
        m_TargetPosition = Vector3.zero;

        m_CurrentLane = k_StartingLane;
        characterCollider.transform.localPosition = Vector3.zero;
        IPlayerStateProvider.Instance.Reset();
        m_Audio = GetComponent<AudioSource>();

        m_ObstacleLayer = 1 << LayerMask.NameToLayer("Obstacle");
    }

    // Called at the beginning of a run or rerun
    public void Begin()
    {
        m_IsRunning = false;
        character.animator.SetBool(s_DeadHash, false);

        characterCollider.Init();

        consumables.Clear();
    }

    public void End()
    {
        CleanConsumable();
    }

    public void CleanConsumable()
    {
        for (int i = consumables.Count - 1; i >= 0; i--)
        {
            consumables[i].Ended(this);
        }

        consumables.Clear();
    }

    public void StartRunning()
    {
        StartMoving();
        if (character.animator)
        {
            character.animator.Play(s_RunStartHash);
            character.animator.SetBool(s_MovingHash, true);
        }
    }

    public void StartMoving()
    {
        m_IsRunning = true;
    }

    public void StopMoving()
    {
        m_IsRunning = false;
        trackManager.StopMove();
        if (character != null && character.animator != null)
        {
            character.animator.SetBool(s_MovingHash, false);
        }
    }


    // Event handlers for centralized input - translate to character actions
    private void OnLeftInput()
    {
        // Only respond if game is not paused
        if (_gamePauser.IsPaused) return;

        ChangeLane(-1); // Left input = move left lane
    }

    private void OnRightInput()
    {
        // Only respond if game is not paused
        if (_gamePauser.IsPaused) return;

        ChangeLane(1); // Right input = move right lane
    }

    private void OnUpInput()
    {
        // Only respond if game is not paused
        if (_gamePauser.IsPaused) return;

        Jump(); // Up input = jump
    }

    private void OnDownInput()
    {
        // Only respond if game is not paused
        if (_gamePauser.IsPaused) return;

        if (!m_Sliding)
            Slide(); // Down input = slide
    }

    protected void Update()
    {
        // Input is now handled by GameInputController through events
        // This Update method now only handles movement physics and animations

        Vector3 verticalTargetPosition = m_TargetPosition;

        if (m_Sliding)
        {
            // Slide time isn't constant but the slide length is (even if slightly modified by speed, to slide slightly further when faster).
            // This is for gameplay reason, we don't want the character to drasticly slide farther when at max speed.
            float correctSlideLength = slideLength * (1.0f + trackManager.speedRatio);
            float ratio = (trackManager.worldDistance - m_SlideStart) / correctSlideLength;
            if (ratio >= 1.0f)
            {
                // We slid to (or past) the required length, go back to running
                StopSliding();
            }
        }

        if (m_Jumping)
        {
            if (trackManager.isMoving)
            {
                // Same as with the sliding, we want a fixed jump LENGTH not fixed jump TIME. Also, just as with sliding,
                // we slightly modify length with speed to make it more playable.
                float correctJumpLength = jumpLength * (1.0f + trackManager.speedRatio);

                float ratio = (trackManager.worldDistance - m_JumpStart) / correctJumpLength;
                if (ratio >= 1.0f)
                {
                    m_Jumping = false;
                    character.animator.SetBool(s_JumpingHash, false);
                }
                else
                {
                    verticalTargetPosition.y = Mathf.Sin(ratio * Mathf.PI) * jumpHeight;
                }
            }
            else if
                (!_gamePauser.IsPaused)
            {
                verticalTargetPosition.y =
                    Mathf.MoveTowards(verticalTargetPosition.y, 0, k_GroundingSpeed * Time.deltaTime);
                if (Mathf.Approximately(verticalTargetPosition.y, 0f))
                {
                    character.animator.SetBool(s_JumpingHash, false);
                    m_Jumping = false;
                }
            }
        }

        characterCollider.transform.localPosition = Vector3.MoveTowards(characterCollider.transform.localPosition,
            verticalTargetPosition, laneChangeSpeed * Time.deltaTime);

        // Put blob shadow under the character.
        if (Physics.Raycast(characterCollider.transform.position + Vector3.up, Vector3.down, out var hit,
                k_ShadowRaycastDistance, m_ObstacleLayer))
        {
            blobShadow.transform.position = hit.point + Vector3.up * k_ShadowGroundOffset;
        }
        else
        {
            Vector3 shadowPosition = characterCollider.transform.position;
            shadowPosition.y = k_ShadowGroundOffset;
            blobShadow.transform.position = shadowPosition;
        }
    }

    public void Jump()
    {
        if (!m_IsRunning)
            return;

        if (!m_Jumping)
        {
            if (m_Sliding)
                StopSliding();

            float correctJumpLength = jumpLength * (1.0f + trackManager.speedRatio);
            m_JumpStart = trackManager.worldDistance;

            float baseJumpRatio = ITrackRunnerConfigProvider.Instance.JumpAnimSpeedRatio;
            float animSpeed = baseJumpRatio * (trackManager.speed / correctJumpLength);

            character.animator.SetFloat(s_JumpingSpeedHash, animSpeed);
            character.animator.SetBool(s_JumpingHash, true);
            m_Audio.PlayOneShot(character.jumpSound);
            m_Jumping = true;
            OnJumped?.Invoke();
        }
    }

    public void StopJumping()
    {
        if (m_Jumping)
        {
            character.animator.SetBool(s_JumpingHash, false);
            m_Jumping = false;
        }
    }

    public void Slide()
    {
        if (!m_IsRunning || m_Sliding)
            return;

        if (m_Jumping)
            StopJumping();

        float correctSlideLength = slideLength * (1.0f + trackManager.speedRatio);
        m_SlideStart = trackManager.worldDistance;

        float baseSlideRatio = ITrackRunnerConfigProvider.Instance.SlideAnimSpeedRatio;
        float animSpeed = baseSlideRatio * (trackManager.speed / correctSlideLength);

        character.animator.SetFloat(s_JumpingSpeedHash, animSpeed);
        character.animator.SetBool(s_SlidingHash, true);
        m_Audio.PlayOneShot(slideSound);
        m_Sliding = true;

        characterCollider.Slide(true);
        OnDucked?.Invoke();
    }

    public void StopSliding()
    {
        if (m_Sliding)
        {
            character.animator.SetBool(s_SlidingHash, false);
            m_Sliding = false;

            characterCollider.Slide(false);
        }
    }

    public void ChangeLane(int direction)
    {
        if (!m_IsRunning)
            return;

        int targetLane = m_CurrentLane + direction;

        if (targetLane is < 0 or > 2)
            // Ignore, we are on the borders.
            return;

        bool isBlocked = consumables.Count > 0 && consumables.Any(o => o is Shackle);
        OnSwitchLaneAttempt?.Invoke(isBlocked == false);
        if (isBlocked)
        {
            // Lane switching is blocked
            return;
        }

        if (m_CurrentLane == targetLane)
        {
            return;
        }

        bool isLeft = direction < 0;

        m_CurrentLane = targetLane;
        m_TargetPosition = new Vector3((m_CurrentLane - 1) * trackManager.laneOffset, 0, 0);

        OnLaneSwitched?.Invoke(isLeft);
    }

    public void UseInventory()
    {
        if (inventory != null && inventory.CanBeUsed(this))
        {
            UseConsumable(inventory);
            inventory = null;
        }
    }

    public void UseConsumable(Consumable c)
    {
        characterCollider.audio.PlayOneShot(powerUpUseSound);

        for (int i = 0; i < consumables.Count; ++i)
        {
            if (consumables[i].GetConsumableType() == c.GetConsumableType())
            {
                // If we already have an active consumable of that type, we just reset the time
                consumables[i].ResetTime();
                _lootablesSpawner.RemovePowerUp(c);
                return;
            }
        }


        c.Owner = this;

        // If we didn't had one, activate that one 
        c.transform.SetParent(transform, true);
        c.transform.position = Vector3.one * 9999;

        consumables.Add(c);
        c.StartAsync(this).Forget();
    }
}