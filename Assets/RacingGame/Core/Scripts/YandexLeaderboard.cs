// ═══════════════════════════════════════════════════════════════
//  YandexLeaderboard.cs  —  Отправка рекордов в Яндекс Games
//
//  Требует: Яндекс Games SDK (com.yandex.games)
//  https://github.com/forcepusher/com.agava.yandexgames
//
//  • Слушает GameEventBus.OnGameOver, отправляет счёт
//  • Слушает GameEventBus.OnSubmitScore для ручного вызова
//  • Хранит локальный рекорд в PlayerPrefs
//  • WebGL-only: #if UNITY_WEBGL обёртка
// ═══════════════════════════════════════════════════════════════
using UnityEngine;

// Подключи SDK: using Agava.YandexGames;
// Раскомментируй строки с YandexGamesSdk когда SDK установлен.

public class YandexLeaderboard : MonoBehaviour
{
    [Header("Настройки")]
    [SerializeField] private string leaderboardName = "TopDistance";

    private const string BestScoreKey = "BestScore";
    private int _sessionScore;

    void Awake()
    {
        GameEventBus.OnGameOver      += OnGameOver;
        GameEventBus.OnSubmitScore   += TrySubmitScore;
        GameEventBus.OnScoreChanged  += s => _sessionScore = s;
    }

    void OnDestroy()
    {
        GameEventBus.OnGameOver    -= OnGameOver;
        GameEventBus.OnSubmitScore -= TrySubmitScore;
    }

    // ── Инициализация SDK (вызвать из стартового MonoBehaviour) ─
    public static void InitYandexSDK(System.Action onReady = null)
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        // YandexGamesSdk.Initialize(onReady);
        Debug.Log("[YandexLeaderboard] SDK Initialize → раскомментируй после установки пакета.");
#endif
        onReady?.Invoke();
    }

    // ── Отправка очков ─────────────────────────────────────────
    private void OnGameOver()
    {
        TrySubmitScore(_sessionScore);
    }

    private void TrySubmitScore(int score)
    {
        // Сохраняем локальный рекорд всегда
        int best = PlayerPrefs.GetInt(BestScoreKey, 0);
        if (score > best)
        {
            PlayerPrefs.SetInt(BestScoreKey, score);
            PlayerPrefs.Save();
        }

#if UNITY_WEBGL && !UNITY_EDITOR
        SubmitToYandex(score);
#else
        Debug.Log($"[YandexLeaderboard] (Editor) Score = {score}, Best = {PlayerPrefs.GetInt(BestScoreKey)}");
#endif
    }

    private void SubmitToYandex(int score)
    {
        // Раскомментируй после установки Яндекс Games SDK:
        // if (!PlayerAccount.IsAuthorized) return;
        // Leaderboard.SetScore(leaderboardName, score,
        //     () => Debug.Log("[YandexLeaderboard] Score submitted!"),
        //     err => Debug.LogWarning($"[YandexLeaderboard] Error: {err}"));
        Debug.Log($"[YandexLeaderboard] WebGL submit: {score} → {leaderboardName}");
    }

    // ── Получение рекорда ──────────────────────────────────────
    public int GetLocalBest() => PlayerPrefs.GetInt(BestScoreKey, 0);
}
