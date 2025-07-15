using System;
using UnityEngine;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

/// <summary>
/// Defines a consumable (called "power up" in game). Each consumable is derived from this and implements its functions.
/// </summary>
public abstract class Consumable : MonoBehaviour
{
    public delegate void ConsumableActiveStateChangeEventArgs(Consumable consumable);

    public float duration;

    public enum ConsumableType
    {
        NONE,
        COIN_MAG,
        SCORE_MULTIPLAYER,
        INVINCIBILITY,
        EXTRALIFE,
        SHACKLE,
        MAX_COUNT,
        SHIELD,
    }

    public Sprite icon;

    public AudioClip activatedSound;

    //public ParticleSystem activatedParticle;
    public AssetReference ActivatedParticleReference;
    public bool canBeSpawned = true;

    public bool active { get; private set; } = false;

    private float m_SinceStart = 0;

    protected ParticleSystem m_ParticleSpawned;

    // Here - for the sake of showing diverse way of doing things - we use abstract functions to get the data for each consumable.
    // Another way to do it would be to have public field, like the Character or Accesories use, and define all those on the prefabs instead of here.
    // This method allows information to be all in code (so no need for prefab etc.) the other make it easier to modify without recompiling/by non-programmer.
    public abstract ConsumableType GetConsumableType();
    public abstract string GetConsumableName();
    public abstract int GetPrice();
    public abstract int GetPremiumCost();

    public CharacterInputController Owner { get; set; }

    public static event ConsumableActiveStateChangeEventArgs OnConsumableActiveStateChange;

    private void OnDisable()
    {
        Debug.Log($"[Consumables] Disabling {name}");
    }

    private void Update()
    {
        CheckAndExpire();
    }

    private void CheckAndExpire()
    {
        if (active == false)
        {
            return;
        }

        if (TrackManager.instance == null)
        {
            return;
        }

        if (TrackManager.instance.characterController == null)
        {
            return;
        }

        Tick(TrackManager.instance.characterController);
        if (IsExpired())
        {
            Ended(TrackManager.instance.characterController);
        }
    }

    private bool IsExpired() => m_SinceStart >= duration;

    public void ResetTime()
    {
        m_SinceStart = 0;
    }

    //override this to do test to make a consumable not usable (e.g. used by the ExtraLife to avoid using it when at full health)
    public virtual bool CanBeUsed(CharacterInputController c)
    {
        return true;
    }

    protected virtual async UniTask StartInternalAsync(CharacterInputController c)
    {
        m_SinceStart = 0;
        SetActivated(true);

        if (activatedSound != null)
        {
            c.powerupSource.clip = activatedSound;
            c.powerupSource.Play();
        }

        if (ActivatedParticleReference != null)
        {
            //Addressables 1.0.1-preview
            var op = ActivatedParticleReference.InstantiateAsync();
            await op;
            if (op.Status == AsyncOperationStatus.Succeeded)
            {
                m_ParticleSpawned = op.Result.GetComponent<ParticleSystem>();
                if (!m_ParticleSpawned.main.loop)
                {
                    TimedRelease(m_ParticleSpawned.gameObject, m_ParticleSpawned.main.duration);
                }

                m_ParticleSpawned.transform.SetParent(c.characterCollider.transform);
                m_ParticleSpawned.transform.localPosition = op.Result.transform.position;
            }
            else
            {
                Debug.LogError($"Failed to spawn collectable spawned particles: {GetConsumableType()}");
            }
        }
    }

    public async UniTaskVoid StartAsync(CharacterInputController c)
    {
        try
        {
            await StartInternalAsync(c);
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
        }
    }

    private static void TimedRelease(GameObject obj, float time)
    {
        DOVirtual.DelayedCall(time,
            () => { Addressables.ReleaseInstance(obj); });
    }

    protected virtual void TickInternal(CharacterInputController c)
    {
        // By default do nothing, override to do per frame manipulation
        m_SinceStart += Time.deltaTime;
    }

    protected virtual void DoEnd(CharacterInputController c)
    {
        if (m_ParticleSpawned != null)
        {
            if (m_ParticleSpawned.main.loop)
                Addressables.ReleaseInstance(m_ParticleSpawned.gameObject);
        }

        if (activatedSound != null && c.powerupSource.clip == activatedSound)
            c.powerupSource.Stop(); //if this one the one using the audio source stop it

        SetActivated(false);

        for (int i = 0; i < c.consumables.Count; ++i)
        {
            if (c.consumables[i] != null && c.consumables[i].active && c.consumables[i].activatedSound != null)
            {
                //if there is still an active consumable that have a sound, this is the one playing now
                c.powerupSource.clip = c.consumables[i].activatedSound;
                c.powerupSource.Play();
            }
        }
    }

    private void Tick(CharacterInputController c)
    {
        if (active == false)
        {
            return;
        }

        TickInternal(c);
    }

    public void Ended(CharacterInputController c)
    {
        DoEnd(c);
        c.consumables.Remove(this);
        Debug.Log($"[Consumables] Releasing {name}");
        // Self-cleanup via Addressables
        Addressables.ReleaseInstance(gameObject);
    }

    private void SetActivated(bool isActivated)
    {
        active = isActivated;
        OnConsumableActiveStateChange?.Invoke(this);
    }

    public void ForceEnd() => active = false;

    public float GetRemainingLifeNormalized()
    {
        return active ? 1 - m_SinceStart / duration : 1;
    }
}