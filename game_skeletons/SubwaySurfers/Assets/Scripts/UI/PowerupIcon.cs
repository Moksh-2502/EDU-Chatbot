using UnityEngine;
using UnityEngine.UI;
using SubwaySurfers.Runtime;
using System.Collections;

public class PowerupIcon : MonoBehaviour
{
    [SerializeField] private RectTransform anchor;
    public Consumable DataContext { get; private set; }

    public Image icon;
    public Slider slider;

    private Vector3 _originalScale;
    private Quaternion _originalRotation;

    private bool _isInitialized;

    private void EnsureInitialized()
    {
        if (_isInitialized)
        {
            return;
        }

        _isInitialized = true;

        _originalScale = anchor.localScale;
        _originalRotation = anchor.localRotation;
    }

    public void Repaint(Consumable consumable)
    {
        EnsureInitialized();
        if (DataContext != null && DataContext.Owner != null)
        {
            DataContext.Owner.OnSwitchLaneAttempt -= HandleLaneSwitchAttempt;
        }

        DataContext = consumable;
        icon.sprite = consumable.icon;
        if (DataContext != null && DataContext.Owner != null)
        {
            DataContext.Owner.OnSwitchLaneAttempt += HandleLaneSwitchAttempt;
        }

        anchor.localScale = _originalScale;
        anchor.localRotation = _originalRotation;
    }

    private void Update()
    {
        if (_isInitialized == false || DataContext == null)
        {
            return;
        }

        slider.value = DataContext.GetRemainingLifeNormalized();
    }

    private void HandleLaneSwitchAttempt(bool success)
    {
        // If lane switch failed and this is a shackle powerup, show shake animation
        if (!success && DataContext is Shackle)
        {
            StartCoroutine(ShakeAnimation());
        }
    }

    private IEnumerator ShakeAnimation()
    {
        // Animation parameters
        float shakeDuration = 0.5f;
        float shakeIntensity = 15f;
        float scaleMultiplier = 1.2f;

        float elapsed = 0f;

        // Shake and scale animation
        while (elapsed < shakeDuration)
        {
            // Calculate shake rotation
            float shakeAngle = Random.Range(-shakeIntensity, shakeIntensity);
            Quaternion targetRotation = Quaternion.Euler(0, 0, shakeAngle);

            // Calculate scale
            float scaleProgress = Mathf.PingPong(elapsed * 4, 1.0f);
            float currentScale = Mathf.Lerp(1.0f, scaleMultiplier, scaleProgress);
            Vector3 targetScale = _originalScale * currentScale;

            // Apply rotation and scale
            anchor.localRotation = targetRotation;
            anchor.localScale = targetScale;

            elapsed += Time.deltaTime;
            yield return null;
        }

        // Return to original state
        anchor.localRotation = _originalRotation;
        anchor.localScale = _originalScale;
    }
}