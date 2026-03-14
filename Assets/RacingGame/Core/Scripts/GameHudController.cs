using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GameHudController : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI scoreLabel;
    [SerializeField] private TextMeshProUGUI statusLabel;
    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private TextMeshProUGUI gameOverTitle;
    [SerializeField] private TextMeshProUGUI gameOverStats;
    [SerializeField] private Button runAgainButton;
    [SerializeField] private Button mainMenuButton;

    private GameManager _gameManager;
    private int _score;

    private void Awake()
    {
        _gameManager = FindFirstObjectByType<GameManager>();

        GameEventBus.OnScoreChanged += HandleScoreChanged;
        GameEventBus.OnBiomeChanged += HandleBiomeChanged;
        GameEventBus.OnGameOver += HandleGameOver;
        GameEventBus.OnGameRestart += ResetHud;

        WireButtons();
        ResetHud();
    }

    private void OnDestroy()
    {
        GameEventBus.OnScoreChanged -= HandleScoreChanged;
        GameEventBus.OnBiomeChanged -= HandleBiomeChanged;
        GameEventBus.OnGameOver -= HandleGameOver;
        GameEventBus.OnGameRestart -= ResetHud;
    }

    public void Configure(
        TextMeshProUGUI configuredScoreLabel,
        TextMeshProUGUI configuredStatusLabel,
        GameObject configuredGameOverPanel,
        TextMeshProUGUI configuredGameOverTitle,
        TextMeshProUGUI configuredGameOverStats,
        Button configuredRunAgainButton,
        Button configuredMainMenuButton)
    {
        scoreLabel = configuredScoreLabel;
        statusLabel = configuredStatusLabel;
        gameOverPanel = configuredGameOverPanel;
        gameOverTitle = configuredGameOverTitle;
        gameOverStats = configuredGameOverStats;
        runAgainButton = configuredRunAgainButton;
        mainMenuButton = configuredMainMenuButton;
        WireButtons();
        ResetHud();
    }

    private void HandleScoreChanged(int score)
    {
        _score = score;
        if (scoreLabel != null)
            scoreLabel.text = score.ToString("N0");
    }

    private void HandleBiomeChanged(BiomeZone zone)
    {
        if (statusLabel != null)
            statusLabel.text = $"{zone.name.ToUpperInvariant()} LINK";
    }

    private void HandleGameOver()
    {
        if (gameOverPanel != null)
            gameOverPanel.SetActive(true);

        if (gameOverTitle != null)
            gameOverTitle.text = "System Failure";

        if (gameOverStats != null && _gameManager != null)
        {
            gameOverStats.text =
                $"Score: {_score:N0}\nDistance: {Mathf.FloorToInt(_gameManager.DistanceMeters)} m\nRAM stability: 0%";
        }
    }

    private void ResetHud()
    {
        _score = 0;

        if (scoreLabel != null)
            scoreLabel.text = "0";

        if (statusLabel != null)
            statusLabel.text = "CITY LINK";

        if (gameOverPanel != null)
            gameOverPanel.SetActive(false);
    }

    private void RestartRun()
    {
        if (_gameManager != null)
            _gameManager.RestartRun();
    }

    private void ReturnToMainMenu()
    {
        if (_gameManager != null)
            _gameManager.ReturnToMainMenu();
    }

    private void WireButtons()
    {
        if (runAgainButton != null)
        {
            runAgainButton.onClick.RemoveListener(RestartRun);
            runAgainButton.onClick.AddListener(RestartRun);
        }

        if (mainMenuButton != null)
        {
            mainMenuButton.onClick.RemoveListener(ReturnToMainMenu);
            mainMenuButton.onClick.AddListener(ReturnToMainMenu);
        }
    }
}
