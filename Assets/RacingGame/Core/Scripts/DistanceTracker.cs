// ═══════════════════════════════════════════════════════════════
//  DistanceTracker.cs  —  Считает и показывает дистанцию
//
//  • Подписывается на GameEventBus.OnDistanceChanged
//  • Никаких прямых ссылок на RoadManager
//  • UI обновляется только при реальном изменении
// ═══════════════════════════════════════════════════════════════
using UnityEngine;
using TMPro;

public class DistanceTracker : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI distanceLabel;
    [SerializeField] private TextMeshProUGUI speedLabel;

    private float _distanceMeters;
    private float _bestMeters;

    void Awake()
    {
        GameEventBus.OnDistanceChanged += OnDistanceChanged;
        GameEventBus.OnSpeedChanged    += OnSpeedChanged;
        GameEventBus.OnGameRestart     += ResetUI;
    }

    void OnDestroy()
    {
        GameEventBus.OnDistanceChanged -= OnDistanceChanged;
        GameEventBus.OnSpeedChanged    -= OnSpeedChanged;
        GameEventBus.OnGameRestart     -= ResetUI;
    }

    public void Configure(TextMeshProUGUI distanceText, TextMeshProUGUI speedText)
    {
        distanceLabel = distanceText;
        speedLabel = speedText;
        ResetUI();
    }

    // ── Обработчики ────────────────────────────────────────────
    private void OnDistanceChanged(float meters)
    {
        _distanceMeters = meters;

        if (meters > _bestMeters) _bestMeters = meters;

        if (distanceLabel != null)
            distanceLabel.text = FormatDistance(meters);
    }

    private void OnSpeedChanged(float kmh)
    {
        if (speedLabel != null)
            speedLabel.text = $"{Mathf.RoundToInt(kmh)} км/ч";
    }

    // ── Публичный доступ ───────────────────────────────────────
    public float GetDistance()     => _distanceMeters;
    public float GetBestDistance() => _bestMeters;

    // ── Вспомогательные ───────────────────────────────────────
    private string FormatDistance(float m)
    {
        if (m >= 1000f) return $"{m / 1000f:F2} км";
        return $"{Mathf.FloorToInt(m)} м";
    }

    private void ResetUI()
    {
        _distanceMeters = 0f;
        if (distanceLabel != null) distanceLabel.text = "0 м";
        if (speedLabel    != null) speedLabel.text    = "0 км/ч";
    }
}
