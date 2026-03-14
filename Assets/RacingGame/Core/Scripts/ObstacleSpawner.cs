// ═══════════════════════════════════════════════════════════════
//  ObstacleSpawner.cs  —  Процедурный спавн препятствий
//
//  • Слушает GameEventBus.OnRoadSpawned
//  • Выбирает паттерн на основе DifficultyManager.DifficultyLevel
//  • Использует простой GameObject пул для препятствий
//  • Препятствия — дети платформы, умирают вместе с ней
// ═══════════════════════════════════════════════════════════════
using System.Collections.Generic;
using UnityEngine;

public class ObstacleSpawner : MonoBehaviour
{
    [SerializeField] private List<RoadBiomeData> biomes; // те же данные что у RoadManager

    private Dictionary<GameObject, Queue<GameObject>> _obstaclePool
        = new Dictionary<GameObject, Queue<GameObject>>();

    void Awake()
    {
        GameEventBus.OnRoadSpawned += OnRoadSpawned;
    }

    void OnDestroy()
    {
        GameEventBus.OnRoadSpawned -= OnRoadSpawned;
    }

    public void Configure(List<RoadBiomeData> configuredBiomes)
    {
        biomes = configuredBiomes;
    }

    // ── Обработчик ────────────────────────────────────────────
    private void OnRoadSpawned(Road road)
    {
        var biomeData = FindBiomeData(road.BiomeIndex);
        if (biomeData == null || biomeData.obstacleSets == null) return;

        int diff = DifficultyManager.DifficultyLevel;

        foreach (var set in biomeData.obstacleSets)
        {
            if (set == null) continue;
            if (diff < set.minDifficultyLevel) continue;
            if (set.patterns == null || set.patterns.Length == 0) continue;
            if (Random.value > set.spawnChance) continue;

            // Выбираем случайный паттерн
            var pattern = set.patterns[Random.Range(0, set.patterns.Length)];
            SpawnObstacle(pattern, road.transform);
            break; // Одно препятствие на платформу (можно убрать для хаоса)
        }
    }

    // ── Спавн с пулингом ──────────────────────────────────────
    private void SpawnObstacle(GameObject prefab, Transform parent)
    {
        if (!_obstaclePool.ContainsKey(prefab))
            _obstaclePool[prefab] = new Queue<GameObject>();

        GameObject obj;
        var queue = _obstaclePool[prefab];

        if (queue.Count > 0)
        {
            obj = queue.Dequeue();
            obj.SetActive(true);
        }
        else
        {
            obj = Instantiate(prefab);
        }

        // Крепим к платформе — уедет вместе с ней
        obj.transform.SetParent(parent);
        obj.transform.localPosition = Vector3.zero;
        obj.transform.localRotation = Quaternion.identity;

        // Когда платформа умрёт — препятствие вернётся в пул
        // (через Obstacle.cs, см. ниже)
    }

    // ── Возврат в пул ─────────────────────────────────────────
    public void ReturnObstacle(GameObject prefab, GameObject instance)
    {
        instance.SetActive(false);
        instance.transform.SetParent(transform);

        if (!_obstaclePool.ContainsKey(prefab))
            _obstaclePool[prefab] = new Queue<GameObject>();

        _obstaclePool[prefab].Enqueue(instance);
    }

    // ── Вспомогательный поиск ─────────────────────────────────
    private RoadBiomeData FindBiomeData(int index)
    {
        foreach (var b in biomes)
            if (b != null && b.biomeIndex == index) return b;
        return null;
    }
}
