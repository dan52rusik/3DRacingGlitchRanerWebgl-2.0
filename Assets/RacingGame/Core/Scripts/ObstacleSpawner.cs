using System.Collections.Generic;
using UnityEngine;

public class ObstacleSpawner : MonoBehaviour
{
    [SerializeField] private List<RoadBiomeData> biomes;
    [SerializeField] private float fallbackSpawnChance = 0.45f;
    [SerializeField] private float laneOffset = 2.4f;

    private readonly Dictionary<GameObject, Queue<GameObject>> _obstaclePool = new Dictionary<GameObject, Queue<GameObject>>();
    private readonly List<GameObject> _runtimeFallbackPrefabs = new List<GameObject>();

    private void Awake()
    {
        GameEventBus.OnRoadSpawned += OnRoadSpawned;
    }

    private void OnDestroy()
    {
        GameEventBus.OnRoadSpawned -= OnRoadSpawned;
    }

    public void Configure(List<RoadBiomeData> configuredBiomes)
    {
        biomes = configuredBiomes;
    }

    private void OnRoadSpawned(Road road)
    {
        if (road.transform.position.z < 30f)
            return;

        RoadBiomeData biomeData = FindBiomeData(road.BiomeIndex);
        if (TrySpawnConfiguredObstacle(road, biomeData))
            return;

        TrySpawnFallbackObstacle(road);
    }

    private bool TrySpawnConfiguredObstacle(Road road, RoadBiomeData biomeData)
    {
        if (biomeData == null || biomeData.obstacleSets == null)
            return false;

        int diff = DifficultyManager.DifficultyLevel;

        foreach (ObstacleSetData set in biomeData.obstacleSets)
        {
            if (set == null || diff < set.minDifficultyLevel)
                continue;

            if (set.patterns == null || set.patterns.Length == 0 || Random.value > set.spawnChance)
                continue;

            GameObject pattern = set.patterns[Random.Range(0, set.patterns.Length)];
            SpawnObstacle(pattern, road.transform, Vector3.zero, Quaternion.identity);
            return true;
        }

        return false;
    }

    private void TrySpawnFallbackObstacle(Road road)
    {
        if (Random.value > fallbackSpawnChance)
            return;

        int obstacleCount = Random.value > 0.72f ? 2 : 1;
        int startLane = Random.Range(0, 3);

        for (int i = 0; i < obstacleCount; i++)
        {
            int lane = (startLane + i) % 3;
            GameObject prefab = GetFallbackPrefab(lane);
            Vector3 localPosition = new Vector3((lane - 1) * laneOffset, 0.7f, Random.Range(-2f, 7f));
            SpawnObstacle(prefab, road.transform, localPosition, Quaternion.Euler(0f, Random.Range(-18f, 18f), 0f));
        }
    }

    private void SpawnObstacle(GameObject prefab, Transform parent, Vector3 localPosition, Quaternion localRotation)
    {
        if (!_obstaclePool.TryGetValue(prefab, out Queue<GameObject> queue))
        {
            queue = new Queue<GameObject>();
            _obstaclePool[prefab] = queue;
        }

        GameObject obj = queue.Count > 0 ? queue.Dequeue() : Instantiate(prefab);
        obj.SetActive(true);
        obj.transform.SetParent(parent, false);
        obj.transform.localPosition = localPosition;
        obj.transform.localRotation = localRotation;
    }

    public void ReturnObstacle(GameObject prefab, GameObject instance)
    {
        instance.SetActive(false);
        instance.transform.SetParent(transform, false);

        if (!_obstaclePool.TryGetValue(prefab, out Queue<GameObject> queue))
        {
            queue = new Queue<GameObject>();
            _obstaclePool[prefab] = queue;
        }

        queue.Enqueue(instance);
    }

    private RoadBiomeData FindBiomeData(int index)
    {
        if (biomes == null)
            return null;

        foreach (RoadBiomeData biome in biomes)
        {
            if (biome != null && biome.biomeIndex == index)
                return biome;
        }

        return null;
    }

    private GameObject GetFallbackPrefab(int lane)
    {
        while (_runtimeFallbackPrefabs.Count <= lane)
        {
            _runtimeFallbackPrefabs.Add(CreateFallbackPrefab(_runtimeFallbackPrefabs.Count));
        }

        return _runtimeFallbackPrefabs[lane];
    }

    private static GameObject CreateFallbackPrefab(int index)
    {
        GameObject obstacle = GameObject.CreatePrimitive(PrimitiveType.Cube);
        obstacle.name = $"RuntimeObstacle_{index}";
        obstacle.transform.localScale = new Vector3(1.3f, 1.4f, 1.3f);

        Renderer rendererRef = obstacle.GetComponent<Renderer>();
        rendererRef.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        rendererRef.receiveShadows = false;
        rendererRef.sharedMaterial.color = index switch
        {
            0 => new Color(0.98f, 0.36f, 0.84f),
            1 => new Color(1f, 0.49f, 0.34f),
            _ => new Color(0.94f, 0.82f, 0.26f),
        };

        obstacle.AddComponent<Obstacle>();
        obstacle.SetActive(false);
        DontDestroyOnLoad(obstacle);
        return obstacle;
    }
}
