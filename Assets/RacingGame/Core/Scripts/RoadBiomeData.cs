// ═══════════════════════════════════════════════════════════════
//  RoadBiomeData.cs  —  ScriptableObject с данными биома
// ═══════════════════════════════════════════════════════════════
using UnityEngine;

[CreateAssetMenu(fileName = "NewRoadBiome", menuName = "Runner/Road Biome Data")]
public class RoadBiomeData : ScriptableObject
{
    [Header("Идентификация")]
    public int    biomeIndex;
    public string biomeName;

    [Header("Визуал")]
    public GameObject roadPrefab;
    public Color      skyColorTop     = Color.cyan;
    public Color      skyColorHorizon = Color.white;
    public Color      fogColor        = Color.grey;
    [Range(0f, 500f)]
    public float      fogDensityStart = 80f;

    [Header("Генерация")]
    [Range(0f, 1f)]
    public float spawnWeight = 0.5f;
    public bool  isHighway;
    public bool  isBridge;

    [Header("Препятствия")]
    public ObstacleSetData[] obstacleSets; // массив наборов препятствий
}
