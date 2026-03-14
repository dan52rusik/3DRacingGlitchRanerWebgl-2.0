// ═══════════════════════════════════════════════════════════════
//  GameEventBus.cs  —  Центральная шина событий
//  Заменяет FindObjectOfType и прямые ссылки между системами.
//  Все системы общаются ТОЛЬКО через события.
// ═══════════════════════════════════════════════════════════════
using System;
using UnityEngine;

public static class GameEventBus
{
    // ── Игровой поток ──────────────────────────────────────────
    public static event Action OnGameStart;
    public static event Action OnGameOver;
    public static event Action OnGameRestart;
    public static event Action OnGamePause;
    public static event Action OnGameResume;

    // ── Прогресс и дистанция ───────────────────────────────────
    /// <param name="float">Дистанция в метрах</param>
    public static event Action<float> OnDistanceChanged;

    /// <param name="int">Текущий счёт</param>
    public static event Action<int>   OnScoreChanged;

    // ── Платформы ──────────────────────────────────────────────
    /// <param name="Road">Платформа, которая прошла мимо игрока</param>
    public static event Action<Road>  OnRoadPassed;

    /// <param name="Road">Платформа заспавнилась</param>
    public static event Action<Road>  OnRoadSpawned;

    // ── Биомы ──────────────────────────────────────────────────
    /// <param name="BiomeZone">Новый активный биом</param>
    public static event Action<BiomeZone> OnBiomeChanged;

    // ── Скорость ───────────────────────────────────────────────
    /// <param name="float">Текущая скорость км/ч</param>
    public static event Action<float> OnSpeedChanged;

    /// <param name="float">Величина буста</param>
    public static event Action<float> OnSpeedBoostTriggered;

    // ── Препятствия ────────────────────────────────────────────
    public static event Action OnObstacleHit;

    // ── Яндекс Leaderboard ────────────────────────────────────
    /// <param name="int">Финальный счёт для отправки</param>
    public static event Action<int> OnSubmitScore;

    // ══════════════════════════════════════════════════════════
    //  Диспетчеры (вызываются системами)
    // ══════════════════════════════════════════════════════════
    public static void Dispatch_GameStart()           => OnGameStart?.Invoke();
    public static void Dispatch_GameOver()            => OnGameOver?.Invoke();
    public static void Dispatch_GameRestart()         => OnGameRestart?.Invoke();
    public static void Dispatch_GamePause()           => OnGamePause?.Invoke();
    public static void Dispatch_GameResume()          => OnGameResume?.Invoke();

    public static void Dispatch_DistanceChanged(float meters)   => OnDistanceChanged?.Invoke(meters);
    public static void Dispatch_ScoreChanged(int score)         => OnScoreChanged?.Invoke(score);

    public static void Dispatch_RoadPassed(Road road)           => OnRoadPassed?.Invoke(road);
    public static void Dispatch_RoadSpawned(Road road)          => OnRoadSpawned?.Invoke(road);

    public static void Dispatch_BiomeChanged(BiomeZone zone)    => OnBiomeChanged?.Invoke(zone);

    public static void Dispatch_SpeedChanged(float speed)       => OnSpeedChanged?.Invoke(speed);
    public static void Dispatch_SpeedBoost(float amount)        => OnSpeedBoostTriggered?.Invoke(amount);

    public static void Dispatch_ObstacleHit()                   => OnObstacleHit?.Invoke();
    public static void Dispatch_SubmitScore(int score)          => OnSubmitScore?.Invoke(score);

    // ══════════════════════════════════════════════════════════
    //  Сброс подписок (вызвать при перезагрузке сцены)
    // ══════════════════════════════════════════════════════════
    public static void ClearAllListeners()
    {
        OnGameStart             = null;
        OnGameOver              = null;
        OnGameRestart           = null;
        OnGamePause             = null;
        OnGameResume            = null;
        OnDistanceChanged       = null;
        OnScoreChanged          = null;
        OnRoadPassed            = null;
        OnRoadSpawned           = null;
        OnBiomeChanged          = null;
        OnSpeedChanged          = null;
        OnSpeedBoostTriggered   = null;
        OnObstacleHit           = null;
        OnSubmitScore           = null;
    }
}
