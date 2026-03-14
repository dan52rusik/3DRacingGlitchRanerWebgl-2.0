// ═══════════════════════════════════════════════════════════════
//  PlatformMovement.cs  —  Движет все платформы назад
//
//  • Скорость берётся из DifficultyManager (не хранит свою)
//  • Подписывается на GameEventBus.OnGameOver для остановки
//  • WebGL: только transform.Translate в Update (не FixedUpdate)
// ═══════════════════════════════════════════════════════════════
using UnityEngine;

public class PlatformMovement : MonoBehaviour
{
    private bool _active = true;

    void Awake()
    {
        GameEventBus.OnGameOver    += () => _active = false;
        GameEventBus.OnGameRestart += () => _active = true;
        GameEventBus.OnGamePause   += () => _active = false;
        GameEventBus.OnGameResume  += () => _active = true;
    }

    void OnDestroy()
    {
        // Лямбды здесь нельзя отписать напрямую,
        // поэтому при перезагрузке сцены вызывается ClearAllListeners()
    }

    void Update()
    {
        if (!_active) return;

        float speedMs = DifficultyManager.CurrentSpeedMs;
        transform.Translate(Vector3.back * speedMs * Time.deltaTime);
    }
}
