using SubwaySurfers;
using UnityEngine;
using UnityEngine.UI;

public class CharacterDeathUI : MonoBehaviour
{
    [SerializeField] private GameObject root, hideOnSeconWindRequested;
    [SerializeField] private Button showQuestionButton;
    private IGameState _gameState;
    private IGameManager _gameManager;
    private void Awake()
    {
        _gameState = FindFirstObjectByType<GameState>(FindObjectsInactive.Include);
        _gameState.OnRunEnd += OnRunEnd;
        _gameState.OnGameStarted += OnGameStarted;
        _gameState.OnSecondWindStarted += OnGameStarted;
        _gameManager = FindFirstObjectByType<GameManager>(FindObjectsInactive.Include);
        if (_gameManager != null)
        {
            _gameManager.OnGameStateChanged += OnGameStateChanged;
        }
        root.SetActive(false);
        showQuestionButton.onClick.AddListener(OnShowQuestionClicked);
    }
    
    private void OnGameStateChanged(AState newState)
    {
        root.SetActive(false);
    }

    private void OnShowQuestionClicked()
    {
        hideOnSeconWindRequested.SetActive(false);
        _gameState.RequestSecondWind();
    }

    private void OnGameStarted()
    {
        root.SetActive(false);
    }

    private void OnRunEnd()
    {
        root.SetActive(true);
        hideOnSeconWindRequested.SetActive(true);
    }
}
