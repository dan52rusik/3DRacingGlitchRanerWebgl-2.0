using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class RacingGameAutoBootstrap : MonoBehaviour
{
    private static bool _installed;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Install()
    {
        if (_installed) return;
        _installed = true;

        var bootstrap = new GameObject(nameof(RacingGameAutoBootstrap));
        bootstrap.AddComponent<RacingGameAutoBootstrap>();
        DontDestroyOnLoad(bootstrap);
    }

    private void Awake()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
        BuildSceneIfNeeded();
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        GameEventBus.ClearAllListeners();
        BuildSceneIfNeeded();
    }

    private void BuildSceneIfNeeded()
    {
        if (FindFirstObjectByType<RoadManager>() != null) return;

        Camera camera = EnsureCamera();
        EnsureDirectionalLight();
        Transform deletionAnchor = EnsureDeletionAnchor(camera.transform);
        List<RoadBiomeData> biomes = CreateRuntimeBiomes(CreateRoadTemplate());
        RoadBiomeSettings settings = CreateBiomeSettings();

        var systems = new GameObject("GameSystems");
        systems.transform.position = Vector3.zero;

        var difficulty = systems.AddComponent<DifficultyManager>();
        var roadManager = systems.AddComponent<RoadManager>();
        var obstacleSpawner = systems.AddComponent<ObstacleSpawner>();
        var visualTransition = systems.AddComponent<BiomeVisualTransition>();
        var leaderboard = systems.AddComponent<YandexLeaderboard>();

        roadManager.Configure(biomes, settings, deletionAnchor);
        obstacleSpawner.Configure(biomes);
        visualTransition.Configure(RenderSettings.skybox, settings.biomeTransitionTime);

        var hud = CreateHud();
        var distanceTracker = hud.Root.AddComponent<DistanceTracker>();
        distanceTracker.Configure(hud.DistanceLabel, hud.SpeedLabel);

        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.Linear;
        RenderSettings.fogEndDistance = 140f;

        YandexLeaderboard.InitYandexSDK();
        GameEventBus.Dispatch_GameStart();
    }

    private static Camera EnsureCamera()
    {
        if (Camera.main != null) return Camera.main;

        var cameraObject = new GameObject("Main Camera");
        cameraObject.tag = "MainCamera";

        var camera = cameraObject.AddComponent<Camera>();
        camera.clearFlags = CameraClearFlags.Skybox;
        camera.fieldOfView = 60f;
        camera.transform.position = new Vector3(0f, 7f, -18f);
        camera.transform.rotation = Quaternion.Euler(12f, 0f, 0f);
        return camera;
    }

    private static Transform EnsureDeletionAnchor(Transform cameraTransform)
    {
        Transform existing = cameraTransform.Find("DeletionAnchor");
        if (existing != null) return existing;

        var anchor = new GameObject("DeletionAnchor").transform;
        anchor.SetParent(cameraTransform, false);
        anchor.localPosition = Vector3.zero;
        anchor.localRotation = Quaternion.identity;
        return anchor;
    }

    private static void EnsureDirectionalLight()
    {
        if (FindFirstObjectByType<Light>() != null) return;

        var lightObject = new GameObject("Directional Light");
        var light = lightObject.AddComponent<Light>();
        light.type = LightType.Directional;
        light.intensity = 1.1f;
        light.transform.rotation = Quaternion.Euler(35f, -25f, 0f);
    }

    private static GameObject CreateRoadTemplate()
    {
        var roadTemplate = GameObject.CreatePrimitive(PrimitiveType.Cube);
        roadTemplate.name = "RuntimeRoadTemplate";
        roadTemplate.transform.localScale = new Vector3(8f, 0.5f, 24f);
        roadTemplate.transform.position = new Vector3(0f, -0.25f, 0f);
        roadTemplate.AddComponent<Road>();
        roadTemplate.AddComponent<PlatformMovement>();

        var renderer = roadTemplate.GetComponent<Renderer>();
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = false;

        roadTemplate.SetActive(false);
        DontDestroyOnLoad(roadTemplate);
        return roadTemplate;
    }

    private static List<RoadBiomeData> CreateRuntimeBiomes(GameObject roadTemplate)
    {
        return new List<RoadBiomeData>
        {
            CreateBiome(0, "City", roadTemplate, new Color(0.18f, 0.45f, 0.95f), new Color(0.85f, 0.92f, 1f), new Color(0.55f, 0.62f, 0.75f), 0.45f),
            CreateBiome(1, "Sunset", roadTemplate, new Color(0.96f, 0.48f, 0.22f), new Color(1f, 0.84f, 0.55f), new Color(0.82f, 0.56f, 0.42f), 0.35f),
            CreateBiome(2, "Night", roadTemplate, new Color(0.05f, 0.08f, 0.2f), new Color(0.18f, 0.24f, 0.38f), new Color(0.12f, 0.16f, 0.25f), 0.2f),
        };
    }

    private static RoadBiomeData CreateBiome(int biomeIndex, string biomeName, GameObject roadTemplate, Color skyTop, Color skyHorizon, Color fogColor, float weight)
    {
        var biome = ScriptableObject.CreateInstance<RoadBiomeData>();
        biome.biomeIndex = biomeIndex;
        biome.biomeName = biomeName;
        biome.roadPrefab = roadTemplate;
        biome.skyColorTop = skyTop;
        biome.skyColorHorizon = skyHorizon;
        biome.fogColor = fogColor;
        biome.fogDensityStart = 140f;
        biome.spawnWeight = weight;
        return biome;
    }

    private static RoadBiomeSettings CreateBiomeSettings()
    {
        var settings = ScriptableObject.CreateInstance<RoadBiomeSettings>();
        settings.defaultBiomeLength = 2.5f;
        settings.minBiomeLength = 0.8f;
        settings.maxBiomeLength = 1.8f;
        settings.minSpacing = 0f;
        settings.maxSpacing = 0.4f;
        settings.biomeTransitionTime = 2.5f;
        settings.lookaheadBiomes = 5;
        return settings;
    }

    private static HudRefs CreateHud()
    {
        var canvasObject = new GameObject("HUD");
        var canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvasObject.AddComponent<CanvasScaler>();
        canvasObject.AddComponent<GraphicRaycaster>();

        var distanceLabel = CreateLabel(canvas.transform, "DistanceLabel", new Vector2(20f, -20f), 42, TextAlignmentOptions.TopLeft);
        var speedLabel = CreateLabel(canvas.transform, "SpeedLabel", new Vector2(20f, -68f), 30, TextAlignmentOptions.TopLeft);

        return new HudRefs(canvasObject, distanceLabel, speedLabel);
    }

    private static TextMeshProUGUI CreateLabel(Transform parent, string objectName, Vector2 anchoredPosition, float fontSize, TextAlignmentOptions alignment)
    {
        var labelObject = new GameObject(objectName);
        labelObject.transform.SetParent(parent, false);

        var rect = labelObject.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = new Vector2(420f, 48f);

        var text = labelObject.AddComponent<TextMeshProUGUI>();
        text.fontSize = fontSize;
        text.alignment = alignment;
        text.color = Color.white;
        text.text = "0";
        return text;
    }

    private readonly struct HudRefs
    {
        public HudRefs(GameObject root, TextMeshProUGUI distanceLabel, TextMeshProUGUI speedLabel)
        {
            Root = root;
            DistanceLabel = distanceLabel;
            SpeedLabel = speedLabel;
        }

        public GameObject Root { get; }
        public TextMeshProUGUI DistanceLabel { get; }
        public TextMeshProUGUI SpeedLabel { get; }
    }
}
