// ═══════════════════════════════════════════════════════════════
//  DifficultyManager.cs  —  Динамическая сложность
//
//  • Скорость растёт по AnimationCurve (легко настроить в Inspector)
//  • Сложность препятствий растёт по дистанции (уровни 0–3)
//  • Speed Boost через GameEventBus
//  • WebGL: никаких тяжёлых FixedUpdate вычислений
// ═══════════════════════════════════════════════════════════════
using UnityEngine;

public class DifficultyManager : MonoBehaviour
{
    // ── Inspector ──────────────────────────────────────────────
    [Header("Скорость")]
    [SerializeField] private float maxSpeedKmh         = 300f;
    [SerializeField] private AnimationCurve speedCurve;     // X = дистанция км, Y = 0..1

    [Header("Буст")]
    [SerializeField] private float boostAmount         = 80f;
    [SerializeField] private float boostDuration       = 5f;

    [Header("Сложность препятствий")]
    [Tooltip("Дистанции (км) для повышения уровня сложности")]
    [SerializeField] private float[] difficultyThresholds = { 0.5f, 1.5f, 3f, 6f };

    // ── Публичные свойства ─────────────────────────────────────
    public static float CurrentSpeedKmh   { get; private set; }
    public static float CurrentSpeedMs    => CurrentSpeedKmh / 3.6f;
    public static int   DifficultyLevel   { get; private set; }

    // ── Приватные поля ─────────────────────────────────────────
    private float _distanceKm;
    private float _boostRemaining;
    private float _lastDispatchedSpeed;

    // ── Unity ──────────────────────────────────────────════════
    void Awake()
    {
        // Дефолтная кривая если не задана в Inspector
        if (speedCurve == null || speedCurve.keys.Length == 0)
        {
            speedCurve = new AnimationCurve(
                new Keyframe(0f,   0.05f),
                new Keyframe(0.5f, 0.25f),
                new Keyframe(2f,   0.55f),
                new Keyframe(5f,   0.80f),
                new Keyframe(10f,  1.00f)
            );
            for (int i = 0; i < speedCurve.keys.Length; i++)
                speedCurve.SmoothTangents(i, 0.5f);
        }

        GameEventBus.OnSpeedBoostTriggered += HandleBoost;
        GameEventBus.OnDistanceChanged     += UpdateDistance;
        GameEventBus.OnGameRestart         += ResetState;
    }

    void OnDestroy()
    {
        GameEventBus.OnSpeedBoostTriggered -= HandleBoost;
        GameEventBus.OnDistanceChanged     -= UpdateDistance;
        GameEventBus.OnGameRestart         -= ResetState;
    }

    void Update()
    {
        UpdateBoost();
        UpdateSpeed();
        UpdateDifficulty();
    }

    // ── Скорость ───────────────────────────────────────────────
    private void UpdateSpeed()
    {
        float t    = speedCurve.Evaluate(_distanceKm);
        float base_ = Mathf.Lerp(0f, maxSpeedKmh, t);

        CurrentSpeedKmh = base_ + (_boostRemaining > 0f ? boostAmount : 0f);

        // Диспатчим только при заметном изменении (экономим события)
        if (Mathf.Abs(CurrentSpeedKmh - _lastDispatchedSpeed) > 0.5f)
        {
            _lastDispatchedSpeed = CurrentSpeedKmh;
            GameEventBus.Dispatch_SpeedChanged(CurrentSpeedKmh);
        }
    }

    // ── Буст ───────────────────────────────────────────────────
    private void HandleBoost(float amount)
    {
        _boostRemaining = boostDuration;
    }

    private void UpdateBoost()
    {
        if (_boostRemaining > 0f)
            _boostRemaining -= Time.deltaTime;
    }

    // ── Сложность препятствий ──────────────────────────────────
    private void UpdateDifficulty()
    {
        for (int i = difficultyThresholds.Length - 1; i >= 0; i--)
        {
            if (_distanceKm >= difficultyThresholds[i])
            {
                DifficultyLevel = i + 1;
                return;
            }
        }
        DifficultyLevel = 0;
    }

    // ── Сброс ──────────────────────────────────────────────────
    private void UpdateDistance(float meters) => _distanceKm = meters / 1000f;

    private void ResetState()
    {
        _distanceKm         = 0f;
        _boostRemaining     = 0f;
        CurrentSpeedKmh     = 0f;
        DifficultyLevel     = 0;
    }
}
