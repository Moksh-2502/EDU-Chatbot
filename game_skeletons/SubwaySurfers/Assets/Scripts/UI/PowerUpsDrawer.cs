using System.Collections.Generic;
using SubwaySurfers;
using UnityEngine;
using UnityEngine.Pool;

namespace UI
{
    public class PowerUpsDrawer : MonoBehaviour
    {
        [SerializeField] private PowerupIcon prefab;
        [SerializeField] private RectTransform container;

        private ObjectPool<PowerupIcon> _iconsPool;
        private readonly List<PowerupIcon> _activeIcons = new();

        private IGameState _gameState;
        private IGameManager _gameManager;

        private void Awake()
        {
            _iconsPool = new ObjectPool<PowerupIcon>(() => Instantiate(prefab, container), fetch =>
            {
                fetch.gameObject.SetActive(true);
            }, action =>
            {
                action.gameObject.SetActive(false);
            }, action => { Destroy(action.gameObject); }, maxSize: 10);

            _gameState = FindFirstObjectByType<GameState>(FindObjectsInactive.Include);
            if (_gameState == null)
            {
                Debug.LogError("No GameState found in the scene.");
                return;
            }

            _gameState.OnGameFinished += OnGameStarted;

            _gameManager = FindFirstObjectByType<GameManager>(FindObjectsInactive.Include);

            if (_gameManager == null)
            {
                Debug.LogError("No GameManager found in the scene.");
                return;
            }

            _gameManager.OnGameStateChanged += OnGameStateChanged;
        }

        private void OnEnable()
        {
            Consumable.OnConsumableActiveStateChange += OnConsumableActiveStateChange;
        }

        private void OnDisable()
        {
            Consumable.OnConsumableActiveStateChange -= OnConsumableActiveStateChange;
        }

        private void OnGameStateChanged(AState state)
        {
            if (state is GameOverState)
            {
                ReleaseAll();
            }
        }

        private void OnGameStarted()
        {
            ReleaseAll();
        }

        private void ReleaseAll()
        {
            foreach (var icon in _activeIcons)
            {
                if (icon != null && icon.gameObject != null)
                {
                    _iconsPool.Release(icon);
                }
            }

            _activeIcons.Clear();
        }


        private void OnConsumableActiveStateChange(Consumable consumable)
        {
            if (consumable.active)
            {
                var icon = _iconsPool.Get();
                icon.Repaint(consumable);
                icon.transform.SetAsLastSibling();
                _activeIcons.Add(icon);
            }
            else
            {
                var iconsToRemove = _activeIcons.FindAll(o => o.DataContext == null || o.DataContext == consumable);
                foreach (var icon in iconsToRemove)
                {
                    _iconsPool.Release(icon);
                    _activeIcons.Remove(icon);
                }
            }
        }
    }
}