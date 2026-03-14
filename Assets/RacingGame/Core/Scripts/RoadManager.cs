// ═══════════════════════════════════════════════════════════════
//  RoadManager.cs  —  Главный спавнер платформ
//
//  Улучшения vs старый код:
//  • Нет FindObjectOfType — только GameEventBus
//  • activePlatforms хранит LinkedList (O(1) удаление головы)
//  • Биом диспетчится при смене через GameEventBus
//  • Нет List.RemoveAt(0) — используем LinkedList.RemoveFirst()
// ═══════════════════════════════════════════════════════════════
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class RoadManager : MonoBehaviour
{
    // ── Inspector ──────────────────────────────────────────────
    [Header("Биомы")]
    [SerializeField] private List<RoadBiomeData> biomes;
    [SerializeField] private RoadBiomeSettings   biomeSettings;

    [Header("Спавн")]
    [SerializeField] private int   maxPlatforms    = 20;
    [SerializeField] private float deletionBuffer  = 25f;   // метров позади игрока
    [SerializeField] private Transform deletionAnchor;      // позиция игрока/камеры

    // ── Приватные поля ─────────────────────────────────────────
    private const float KM = 1000f;

    private Dictionary<int, RoadPool>   _pools          = new Dictionary<int, RoadPool>(8);
    private LinkedList<Road>            _active         = new LinkedList<Road>();
    private BiomeController             _biomeCtrl;
    private bool                        _initialized;

    private float     _totalSpawnedLength;   // метры
    private int       _totalPassed;
    private int       _currentBiomeIndex     = -1;

    // ── Unity ──────────────────────────────────────────────────
    void Awake()
    {
        TryInitialize();
    }

    void Start()
    {
        TryInitialize();
        if (!_initialized) return;

        // Добираем только недостающие стартовые платформы.
        for (int i = _active.Count; i < maxPlatforms; i++)
            SpawnRoad(countProgress: false);
    }

    void Update()
    {
        if (!_initialized)
        {
            TryInitialize();
            if (!_initialized) return;
        }

        TrySpawnRoad();
        RecycleOldRoads();
    }

    public void Configure(List<RoadBiomeData> configuredBiomes, RoadBiomeSettings settings, Transform anchor)
    {
        biomes = configuredBiomes;
        biomeSettings = settings;
        deletionAnchor = anchor;
        TryInitialize();
    }

    // ── Спавн ──────────────────────────────────────────────────
    private void TrySpawnRoad()
    {
        if (_active.Count >= maxPlatforms) return;

        // Спавним пока меньше maxPlatforms
        SpawnRoad(countProgress: true);
    }

    private void SpawnRoad(bool countProgress)
    {
        float km = _totalSpawnedLength / KM;
        var zone = _biomeCtrl.GetBiomeAt(km);

        if (!_pools.TryGetValue(zone.biomeIndex, out var pool))
        {
            Debug.LogWarning($"[RoadManager] Пул для биома {zone.biomeIndex} не найден!");
            return;
        }

        Road road = pool.Get();
        road.transform.position = ComputeSpawnPos(road);
        road.transform.SetParent(transform);
        road.gameObject.SetActive(true);
        road.Initialize(pool, zone.biomeIndex);

        _active.AddLast(road);
        _totalSpawnedLength += road.Length;

        GameEventBus.Dispatch_RoadSpawned(road);

        // Уведомляем о смене биома
        if (zone.biomeIndex != _currentBiomeIndex)
        {
            _currentBiomeIndex = zone.biomeIndex;
            GameEventBus.Dispatch_BiomeChanged(zone);
        }

        if (countProgress)
        {
            _totalPassed++;
            GameEventBus.Dispatch_RoadPassed(road);
            GameEventBus.Dispatch_DistanceChanged(_totalSpawnedLength);
            GameEventBus.Dispatch_ScoreChanged(_totalPassed);
        }
    }

    private Vector3 ComputeSpawnPos(Road next)
    {
        if (_active.Count == 0) return Vector3.zero;

        Road prev  = _active.Last.Value;
        float offZ = (prev.Length + next.Length) * 0.5f;
        return prev.transform.position + new Vector3(0f, 0f, offZ);
    }

    // ── Переработка старых платформ ────────────────────────────
    private void RecycleOldRoads()
    {
        if (_active.Count == 0) return;

        float threshold = deletionAnchor.position.z - deletionBuffer;

        while (_active.Count > 0 && _active.First.Value.transform.position.z < threshold)
        {
            Road road = _active.First.Value;
            _active.RemoveFirst();         // O(1) — никакого сдвига массива
            road.Recycle();
        }
    }

    // ── Инициализация пулов ────────────────────────────────────
    private void InitPools()
    {
        foreach (var biome in biomes)
        {
            if (biome == null || biome.biomeIndex == 3) continue;

            var roadComp = biome.roadPrefab?.GetComponent<Road>();
            if (roadComp == null)
            {
                Debug.LogError($"[RoadManager] Префаб биома '{biome.biomeName}' не содержит компонент Road!");
                continue;
            }

            var pool = gameObject.AddComponent<RoadPool>();
            pool.Initialize(roadComp, 30, biome.biomeIndex);
            _pools[biome.biomeIndex] = pool;
        }
    }

    // ── Публичный API ──────────────────────────────────────────
    public int   InitialPlatformCount => maxPlatforms;
    public float SpawnedLengthMeters  => _totalSpawnedLength;

    public string GetCurrentBiomeName()
    {
        var zone = _biomeCtrl.GetBiomeAt(_totalSpawnedLength / KM);
        return zone?.name ?? "???";
    }

    private void TryInitialize()
    {
        if (_initialized) return;
        if (biomes == null || biomes.Count == 0) return;
        if (biomeSettings == null || deletionAnchor == null) return;

        InitPools();
        _biomeCtrl = new BiomeController(biomes, biomeSettings);
        RegisterExistingRoads();
        _initialized = true;
    }

    private void RegisterExistingRoads()
    {
        _active.Clear();
        _totalSpawnedLength = 0f;

        var roads = GetComponentsInChildren<Road>(includeInactive: true);
        if (roads == null || roads.Length == 0) return;

        System.Array.Sort(roads, (a, b) => a.transform.position.z.CompareTo(b.transform.position.z));

        foreach (var road in roads)
        {
            if (road == null) continue;
            if (!_pools.TryGetValue(road.BiomeIndex, out var pool)) continue;

            road.transform.SetParent(transform);
            road.gameObject.SetActive(true);
            road.Initialize(pool, road.BiomeIndex);
            _active.AddLast(road);
            _totalSpawnedLength += road.Length;
            _currentBiomeIndex = road.BiomeIndex;
        }
    }

    // ── Editor Gizmos ──────────────────────────────────────────
#if UNITY_EDITOR
    void OnDrawGizmos()
    {
        if (_biomeCtrl == null) return;

        foreach (var zone in _biomeCtrl.Zones)
        {
            float zS = zone.startKm * KM;
            float zE = zone.endKm   * KM;
            Gizmos.color = zone.isHighway ? Color.yellow : new Color(0.2f, 1f, 0.4f, 0.8f);
            Gizmos.DrawLine(new Vector3(-8, 0, zS), new Vector3(8, 0, zS));
            Gizmos.DrawLine(new Vector3(-8, 0, zE), new Vector3(8, 0, zE));
            Handles.Label(new Vector3(0, 1, zS), $"{zone.name} [{zone.startKm:F1}–{zone.endKm:F1} km]");
        }
    }
#endif
}
