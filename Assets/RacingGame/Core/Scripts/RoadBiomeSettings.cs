// ═══════════════════════════════════════════════════════════════
//  RoadBiomeSettings.cs  —  Глобальные настройки генерации биомов
// ═══════════════════════════════════════════════════════════════
using UnityEngine;

[CreateAssetMenu(fileName = "BiomeSettings", menuName = "Runner/Biome Settings")]
public class RoadBiomeSettings : ScriptableObject
{
    [Header("Длина биомов (км)")]
    public float defaultBiomeLength    = 10f;
    public float minBiomeLength        = 5f;
    public float maxBiomeLength        = 15f;

    [Header("Расстояние между биомами (км)")]
    public float minSpacing            = 2f;
    public float maxSpacing            = 8f;

    [Header("Визуальный переход (сек)")]
    public float biomeTransitionTime   = 3f;

    [Header("Предгенерация")]
    /// <summary>Сколько биомов генерировать вперёд</summary>
    public int   lookaheadBiomes       = 4;
}
