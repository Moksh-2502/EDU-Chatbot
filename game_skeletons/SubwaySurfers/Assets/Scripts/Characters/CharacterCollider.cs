using System;
using UnityEngine;
using System.Collections.Generic;
using Characters;
using Consumables;
using UnityEngine.AddressableAssets;
using DG.Tweening;
using SubwaySurfers.Tutorial.Core;

// Used mainly by by analytics, but not in an analytics ifdef block 
// so that the data is available to anything (e.g. could be used for player stat saved locally etc.)
public struct DeathEventArgs
{
    public string character;
    public string source;
    public string themeUsed;
    public int coins;
    public int score;
    public float worldDistance;
}

/// <summary>
/// Handles everything related to the collider of the character. This is actually an empty game object, NOT on the character prefab
/// as for gameplay reason, we need a single size collider for every character. (Found on the Main scene PlayerPivot/CharacterSlot gameobject)
/// </summary>
[RequireComponent(typeof(AudioSource))]
public class CharacterCollider : MonoBehaviour
{
    public event Action<string> OnObstacleHit;
    public event Action<GameObject> OnObjectEnteredPlayerTrigger;
    static int s_HitHash = Animator.StringToHash("Hit");
    static int s_DeadHash = Animator.StringToHash("Dead");
    static int s_BlinkingValueHash;

    public CharacterInputController controller;

    public ParticleSystem koParticle;

    [Header("Sound")] public AudioClip coinSound;
    public AudioClip premiumSound;

    public new BoxCollider collider
    {
        get { return m_Collider; }
    }

    public new AudioSource audio
    {
        get { return m_Audio; }
    }

    [HideInInspector] public List<GameObject> magnetCoins = new List<GameObject>();


    protected bool m_Invincible;
    protected bool m_Shielded;
    protected BoxCollider m_Collider;
    protected AudioSource m_Audio;

    protected float m_StartingColliderHeight;
    protected Tween m_InvincibilityTween;
    protected Tween m_BlinkTween;

    protected readonly Vector3 k_SlidingColliderScale = new Vector3(1.0f, 0.5f, 1.0f);
    protected readonly Vector3 k_NotSlidingColliderScale = new Vector3(1.0f, 2.0f, 1.0f);

    protected const float k_MagnetSpeed = 10f;
    protected const int k_CoinsLayerIndex = 8;
    protected const int k_ObstacleLayerIndex = 9;
    public const int k_PowerupLayerIndex = 10;
    protected const float k_DefaultInvinsibleTime = 2f;
    protected const float k_BlinkPeriod = 0.1f;

    public event Action<DeathEventArgs> OnDeath;

    public DeathEventArgs deathData { get; private set; }

    protected void Start()
    {
        m_Collider = GetComponent<BoxCollider>();
        m_Audio = GetComponent<AudioSource>();
        m_StartingColliderHeight = m_Collider.bounds.size.y;
    }

    public void Init()
    {
        koParticle.gameObject.SetActive(false);

        s_BlinkingValueHash = Shader.PropertyToID("_BlinkingValue");
        m_Invincible = false;
        m_Shielded = false;
    }

    public void Slide(bool sliding)
    {
        if (sliding)
        {
            m_Collider.size = Vector3.Scale(m_Collider.size, k_SlidingColliderScale);
            m_Collider.center = m_Collider.center - new Vector3(0.0f, m_Collider.size.y * 0.5f, 0.0f);
        }
        else
        {
            m_Collider.center = m_Collider.center + new Vector3(0.0f, m_Collider.size.y * 0.5f, 0.0f);
            m_Collider.size = Vector3.Scale(m_Collider.size, k_NotSlidingColliderScale);
        }
    }

    protected void Update()
    {
        // Every coin registered to the magnetCoin list (used by the magnet powerup exclusively, but could be used by other power up) is dragged toward the player.
        for (int i = 0; i < magnetCoins.Count; ++i)
        {
            magnetCoins[i].transform.position = Vector3.MoveTowards(magnetCoins[i].transform.position,
                transform.position, k_MagnetSpeed * Time.deltaTime);
        }
    }

    private void ProcessCoinCollision(Collider c)
    {
        if (magnetCoins.Contains(c.gameObject))
            magnetCoins.Remove(c.gameObject);
        if (c.TryGetComponent<Coin>(out var coin))
        {
            var coinRewardAmount = coin.isPremium ? coin.premiumCoinAmount : 1;
            var soundToPlay = coin.isPremium ? premiumSound : coinSound;
            if (coin.isPremium)
            {
                Addressables.ReleaseInstance(c.gameObject);
            }
            else
            {
                Coin.coinPool.Free(c.gameObject);
            }

            IPlayerStateProvider.Instance.ProcessPickedCoins(coinRewardAmount);
            m_Audio.PlayOneShot(soundToPlay);
        }
    }

    private void ProcessObstacleCollision(Collider c)
    {
        if (c.TryGetComponent<Obstacle>(out var obstacle) == false)
        {
            Addressables.ReleaseInstance(c.gameObject);
            return;
        }
        c.enabled = false;
        obstacle.Impacted();
        // If shielded, break the shield instead of taking life
        if (m_Shielded)
        {
            
            // Break the shield by removing the Shield consumable
            BreakShield();
            return;
        }

        TakeLife($"Obstacle: {c.gameObject.name}", ignoreInvincibility: false);
    }
    
    private void ProcessConsumableCollision(Collider c)
    {
        if (!c.TryGetComponent<Consumable>(out var consumable)) return;
        
        // Check for component-based processor first
        var processor = c.GetComponent<Consumables.ConsumableProcessor>();
        if (processor != null && processor.CanProcess(controller))
        {
            processor.ProcessConsumption(consumable, controller);
        }
        else
        {
            // Default behavior - immediate consumption
            controller.UseConsumable(consumable);
        }
    }

    protected void OnTriggerEnter(Collider c)
    {
        if (c.gameObject.layer == k_CoinsLayerIndex)
        {
            ProcessCoinCollision(c);
        }
        else if (c.gameObject.layer == k_ObstacleLayerIndex)
        {
            ProcessObstacleCollision(c);
        }
        else if (c.gameObject.layer == k_PowerupLayerIndex)
        {
            ProcessConsumableCollision(c);
        }

        OnObjectEnteredPlayerTrigger?.Invoke(c.gameObject);
    }

    public bool CanTakeLife() => !m_Invincible && !controller.IsCheatInvincible();

    public void TakeLife(string source, bool ignoreInvincibility = false, float? temporaryInvincibilityDuration = null)
    {
        if (controller.character == null || IPlayerStateProvider.Instance.IsAlive == false)
        {
            return;
        }

        if (ignoreInvincibility == false && CanTakeLife() == false)
            return;

        controller.StopMoving();

        if (ITutorialManager.Instance != null && ITutorialManager.Instance.IsActive)
        {
            // Publish tutorial obstacle hit event for new system
        }
        else
        {
            IPlayerStateProvider.Instance.ChangeLives(-1);
        }

        controller.character.animator.SetTrigger(s_HitHash);

        // Check for active shackle debuff and remove it on collision with obstacle
        OnObstacleHit?.Invoke(source);

        if (IPlayerStateProvider.Instance.IsAlive)
        {
            m_Audio.PlayOneShot(controller.character.hitSound);
            SetInvincible(temporaryInvincibilityDuration ?? k_DefaultInvinsibleTime);
        }
        // The collision killed the player, record all data to analytics.
        else
        {
            m_Audio.PlayOneShot(controller.character.deathSound);
            controller.CleanConsumable();
            controller.character.animator.SetBool(s_DeadHash, true);
            koParticle.gameObject.SetActive(true);
            deathData = new DeathEventArgs
            {
                character = controller.character.characterName,
                themeUsed = controller.trackManager.currentTheme.themeName,
                source = source,
                coins = IPlayerStateProvider.Instance.RunCoins,
                score = IPlayerStateProvider.Instance.RunScore,
                worldDistance = controller.trackManager.worldDistance
            };
            OnDeath?.Invoke(deathData);
        }
    }

    public void SetInvincibleExplicit(bool invincible)
    {
        m_Invincible = invincible;
    }

    public void SetInvincible(float timer = k_DefaultInvinsibleTime)
    {
        StartInvincibilityTimer(timer);
    }

    protected void StartInvincibilityTimer(float timer)
    {
        // Kill any existing invincibility tweens
        KillInvincibilityTweens();

        m_Invincible = true;

        // Create blinking effect tween
        float currentBlink = 1.0f;
        m_BlinkTween = DOTween.To(() => currentBlink, x => currentBlink = x, 0f, k_BlinkPeriod)
            .SetLoops(-1, LoopType.Yoyo)
            .SetEase(Ease.Linear)
            .OnUpdate(() => Shader.SetGlobalFloat(s_BlinkingValueHash, currentBlink));

        // Create main invincibility timer
        m_InvincibilityTween = DOVirtual.DelayedCall(timer, () =>
        {
            // Reset blinking and invincibility
            Shader.SetGlobalFloat(s_BlinkingValueHash, 0.0f);
            m_Invincible = false;
            KillInvincibilityTweens();
        });
    }

    protected void KillInvincibilityTweens()
    {
        if (m_InvincibilityTween != null && m_InvincibilityTween.IsActive())
        {
            m_InvincibilityTween.Kill();
            m_InvincibilityTween = null;
        }

        if (m_BlinkTween != null && m_BlinkTween.IsActive())
        {
            m_BlinkTween.Kill();
            m_BlinkTween = null;
        }
    }

    protected void OnDestroy()
    {
        KillInvincibilityTweens();
    }

    public void SetShielded(bool shielded)
    {
        m_Shielded = shielded;
    }

    public void SetShieldExplicit(bool shielded)
    {
        m_Shielded = shielded;
    }

    public bool IsShielded()
    {
        return m_Shielded;
    }

    protected void BreakShield()
    {
        // Find and remove the shield consumable
        for (int i = controller.consumables.Count - 1; i >= 0; i--)
        {
            if (controller.consumables[i] is Shield)
            {
                controller.consumables[i].Ended(controller);
                return;
            }
        }
    }
}