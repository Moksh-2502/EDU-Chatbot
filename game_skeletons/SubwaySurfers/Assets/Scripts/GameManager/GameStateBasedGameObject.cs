using System;
using System.Linq;
using UnityEngine;

namespace SubwaySurfers
{
    public class GameStateBasedGameObject : MonoBehaviour
    {
        [System.Serializable]
        private class StateBasedObjectData
        {
            [field: SerializeField] public GameObject[] Targets { get; private set; }
            [field: SerializeField] public AState[] States { get; private set; }

            public void HandleState(AState newState)
            {
                var active = States != null && States.Contains(newState);
                foreach (var target in Targets)
                {
                    if (target != null)
                    {
                        target.SetActive(active);
                    }
                }
            }
        }
        
        [SerializeField] private StateBasedObjectData[] targets = Array.Empty<StateBasedObjectData>();

        private IGameManager _gameManager;
        
        private void Awake()
        {
            _gameManager = FindFirstObjectByType<GameManager>(FindObjectsInactive.Include);
        }

        private void OnEnable()
        {
            if (_gameManager != null)
            {
                _gameManager.OnGameStateChanged += OnGameStateChanged;
            }
        }
        
        private void OnDisable()
        {
            if (_gameManager != null)
            {
                _gameManager.OnGameStateChanged -= OnGameStateChanged;
            }
        }
        
        private void OnGameStateChanged(AState newState)
        {
            foreach (var target in targets)
            {
                if (target != null)
                {
                    target.HandleState(newState);
                }
            }
        }
    }
}