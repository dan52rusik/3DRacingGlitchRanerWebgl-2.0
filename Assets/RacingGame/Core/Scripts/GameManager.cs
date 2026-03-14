using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    private bool _gameOver;
    private float _distanceMeters;
    private int _score;

    private void Awake()
    {
        GameEventBus.OnObstacleHit += HandleObstacleHit;
        GameEventBus.OnDistanceChanged += HandleDistanceChanged;
        GameEventBus.OnScoreChanged += HandleScoreChanged;
        GameEventBus.OnGameRestart += HandleGameRestart;
    }

    private void OnDestroy()
    {
        GameEventBus.OnObstacleHit -= HandleObstacleHit;
        GameEventBus.OnDistanceChanged -= HandleDistanceChanged;
        GameEventBus.OnScoreChanged -= HandleScoreChanged;
        GameEventBus.OnGameRestart -= HandleGameRestart;
    }

    public bool IsGameOver => _gameOver;
    public float DistanceMeters => _distanceMeters;
    public int Score => _score;

    public void RestartRun()
    {
        GameEventBus.Dispatch_GameRestart();
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void ReturnToMainMenu()
    {
        RestartRun();
    }

    private void HandleObstacleHit()
    {
        if (_gameOver)
            return;

        _gameOver = true;
        GameEventBus.Dispatch_GameOver();
    }

    private void HandleDistanceChanged(float meters)
    {
        _distanceMeters = meters;
    }

    private void HandleScoreChanged(int score)
    {
        _score = score;
    }

    private void HandleGameRestart()
    {
        _gameOver = false;
        _distanceMeters = 0f;
        _score = 0;
    }
}
