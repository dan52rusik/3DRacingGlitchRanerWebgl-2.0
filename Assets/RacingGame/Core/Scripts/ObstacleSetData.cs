// ═══════════════════════════════════════════════════════════════
//  ObstacleSetData.cs  —  ScriptableObject набора препятствий
// ═══════════════════════════════════════════════════════════════
using UnityEngine;

[CreateAssetMenu(fileName = "ObstacleSet", menuName = "Runner/Obstacle Set")]
public class ObstacleSetData : ScriptableObject
{
    [Header("Общее")]
    public string setName;
    [Range(0, 3)]
    public int minDifficultyLevel;   // появляется только при этом уровне сложности

    [Header("Паттерны (GameObject с Obstacle-компонентом)")]
    public GameObject[] patterns;

    [Header("Настройки спавна")]
    [Range(0f, 1f)]
    public float spawnChance = 0.4f; // вероятность на каждую платформу
}
