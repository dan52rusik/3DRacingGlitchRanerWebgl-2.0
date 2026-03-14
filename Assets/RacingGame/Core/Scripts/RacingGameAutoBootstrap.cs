using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class RacingGameAutoBootstrap : MonoBehaviour
{
    private static bool _installed;
    private static GameObject _runtimeRoadTemplate;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Install()
    {
        if (_installed)
            return;

        _installed = true;

        var bootstrap = new GameObject(nameof(RacingGameAutoBootstrap));
        bootstrap.AddComponent<RacingGameAutoBootstrap>();
        DontDestroyOnLoad(bootstrap);
    }

    private void Awake()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        GameEventBus.ClearAllListeners();
        BuildScene();
    }

    private void BuildScene()
    {
        Camera cameraRef = EnsureCamera();
        EnsureDirectionalLight();
        EnsureEventSystem();
        Transform deletionAnchor = EnsureDeletionAnchor(cameraRef.transform);
        bool hasPreconfiguredRoadManager = FindFirstObjectByType<RoadManager>() != null;

        var systems = FindOrCreateRoot("GameSystems");
        var hudRoot = FindOrCreateRoot("HUD");

        RoadManager roadManager = GetOrAddComponent<RoadManager>(systems);
        GetOrAddComponent<DifficultyManager>(systems);
        ObstacleSpawner obstacleSpawner = GetOrAddComponent<ObstacleSpawner>(systems);
        BiomeVisualTransition visualTransition = GetOrAddComponent<BiomeVisualTransition>(systems);
        GetOrAddComponent<YandexLeaderboard>(systems);
        GetOrAddComponent<GameManager>(systems);

        if (!hasPreconfiguredRoadManager)
        {
            List<RoadBiomeData> biomes = CreateRuntimeBiomes(CreateRoadTemplate());
            RoadBiomeSettings settings = CreateBiomeSettings();
            roadManager.Configure(biomes, settings, deletionAnchor);
            obstacleSpawner.Configure(biomes);
            visualTransition.Configure(RenderSettings.skybox, settings.biomeTransitionTime);
        }

        var hudRefs = EnsureHud(hudRoot.transform);
        DistanceTracker distanceTracker = GetOrAddComponent<DistanceTracker>(hudRoot);
        distanceTracker.Configure(hudRefs.DistanceLabel, hudRefs.SpeedLabel);

        GameHudController hudController = GetOrAddComponent<GameHudController>(hudRoot);
        hudController.Configure(
            hudRefs.ScoreLabel,
            hudRefs.StatusLabel,
            hudRefs.GameOverPanel,
            hudRefs.GameOverTitle,
            hudRefs.GameOverStats,
            hudRefs.RunAgainButton,
            hudRefs.MainMenuButton);

        EnsurePlayer();

        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.Linear;
        RenderSettings.fogEndDistance = 140f;

        YandexLeaderboard.InitYandexSDK();
        GameEventBus.Dispatch_GameStart();
    }

    private static Camera EnsureCamera()
    {
        if (Camera.main != null)
        {
            Camera.main.clearFlags = CameraClearFlags.Skybox;
            Camera.main.fieldOfView = 60f;
            return Camera.main;
        }

        var cameraObject = new GameObject("Main Camera");
        cameraObject.tag = "MainCamera";

        var cameraRef = cameraObject.AddComponent<Camera>();
        cameraRef.clearFlags = CameraClearFlags.Skybox;
        cameraRef.fieldOfView = 60f;
        cameraRef.transform.position = new Vector3(0f, 6.8f, -16.5f);
        cameraRef.transform.rotation = Quaternion.Euler(12f, 0f, 0f);
        return cameraRef;
    }

    private static void EnsureDirectionalLight()
    {
        if (FindFirstObjectByType<Light>() != null)
            return;

        var lightObject = new GameObject("Directional Light");
        var lightRef = lightObject.AddComponent<Light>();
        lightRef.type = LightType.Directional;
        lightRef.intensity = 1.1f;
        lightRef.color = new Color(1f, 0.78f, 0.92f);
        lightRef.transform.rotation = Quaternion.Euler(35f, -25f, 0f);
    }

    private static void EnsureEventSystem()
    {
        if (FindFirstObjectByType<EventSystem>() != null)
            return;

        var system = new GameObject("EventSystem");
        system.AddComponent<EventSystem>();
        system.AddComponent<StandaloneInputModule>();
    }

    private static Transform EnsureDeletionAnchor(Transform cameraTransform)
    {
        Transform existing = cameraTransform.Find("DeletionAnchor");
        if (existing != null)
            return existing;

        var anchor = new GameObject("DeletionAnchor").transform;
        anchor.SetParent(cameraTransform, false);
        anchor.localPosition = Vector3.zero;
        return anchor;
    }

    private static GameObject FindOrCreateRoot(string name)
    {
        GameObject existing = GameObject.Find(name);
        if (existing == null)
            return new GameObject(name);

        return existing;
    }

    private static T GetOrAddComponent<T>(GameObject target) where T : Component
    {
        T component = target.GetComponent<T>();
        return component != null ? component : target.AddComponent<T>();
    }

    private static GameObject CreateRoadTemplate()
    {
        if (_runtimeRoadTemplate != null)
            return _runtimeRoadTemplate;

        var roadTemplate = GameObject.CreatePrimitive(PrimitiveType.Cube);
        roadTemplate.name = "RuntimeRoadTemplate";
        roadTemplate.transform.localScale = new Vector3(8f, 0.5f, 24f);
        roadTemplate.transform.position = new Vector3(0f, -0.25f, 0f);
        roadTemplate.AddComponent<Road>();
        roadTemplate.AddComponent<PlatformMovement>();

        var renderer = roadTemplate.GetComponent<Renderer>();
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        renderer.sharedMaterial.color = new Color(0.08f, 0.09f, 0.14f);

        roadTemplate.SetActive(false);
        DontDestroyOnLoad(roadTemplate);
        _runtimeRoadTemplate = roadTemplate;
        return roadTemplate;
    }

    private static List<RoadBiomeData> CreateRuntimeBiomes(GameObject roadTemplate)
    {
        return new List<RoadBiomeData>
        {
            CreateBiome(0, "City", roadTemplate, new Color(0.1f, 0.72f, 0.95f), new Color(0.35f, 0.95f, 1f), new Color(0.08f, 0.18f, 0.28f), 0.45f),
            CreateBiome(1, "Sunset", roadTemplate, new Color(0.95f, 0.32f, 0.62f), new Color(1f, 0.62f, 0.38f), new Color(0.34f, 0.12f, 0.28f), 0.35f),
            CreateBiome(2, "Night", roadTemplate, new Color(0.03f, 0.05f, 0.14f), new Color(0.22f, 0.12f, 0.32f), new Color(0.04f, 0.08f, 0.16f), 0.2f),
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
        biome.fogDensityStart = 120f;
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

    private static void EnsurePlayer()
    {
        if (FindFirstObjectByType<PlayerController>() != null)
            return;

        var player = new GameObject("Player");
        player.name = "Player";
        player.transform.position = new Vector3(0f, 0.75f, -2f);
        player.transform.localScale = Vector3.one;

        CreateHoverCarVisual(player.transform);
        player.AddComponent<PlayerController>();
    }

    private static void CreateHoverCarVisual(Transform playerRoot)
    {
        var visualRoot = new GameObject("VisualRoot").transform;
        visualRoot.SetParent(playerRoot, false);

        CreateHoverPart("Body", PrimitiveType.Cube, visualRoot, new Vector3(0f, 0f, 0f), new Vector3(1.25f, 0.24f, 2.4f), new Color(0.08f, 0.14f, 0.2f));
        CreateHoverPart("Cabin", PrimitiveType.Cube, visualRoot, new Vector3(0f, 0.22f, 0.15f), new Vector3(0.72f, 0.22f, 0.9f), new Color(0.42f, 0.92f, 1f));
        CreateHoverPart("Nose", PrimitiveType.Cube, visualRoot, new Vector3(0f, 0.05f, 1.15f), new Vector3(0.52f, 0.14f, 0.45f), new Color(0.95f, 0.28f, 0.62f));
        CreateHoverPart("LeftWing", PrimitiveType.Cube, visualRoot, new Vector3(-0.92f, -0.02f, 0.15f), new Vector3(0.42f, 0.06f, 1.3f), new Color(0.18f, 0.82f, 1f));
        CreateHoverPart("RightWing", PrimitiveType.Cube, visualRoot, new Vector3(0.92f, -0.02f, 0.15f), new Vector3(0.42f, 0.06f, 1.3f), new Color(0.18f, 0.82f, 1f));
        CreateHoverPart("RearLeft", PrimitiveType.Cylinder, visualRoot, new Vector3(-0.46f, -0.18f, -1.05f), new Vector3(0.2f, 0.08f, 0.2f), new Color(1f, 0.72f, 0.22f));
        CreateHoverPart("RearRight", PrimitiveType.Cylinder, visualRoot, new Vector3(0.46f, -0.18f, -1.05f), new Vector3(0.2f, 0.08f, 0.2f), new Color(1f, 0.72f, 0.22f));
        CreateHoverPart("GlowLeft", PrimitiveType.Sphere, visualRoot, new Vector3(-0.55f, -0.17f, -1.18f), new Vector3(0.16f, 0.06f, 0.16f), new Color(1f, 0.5f, 0.18f));
        CreateHoverPart("GlowRight", PrimitiveType.Sphere, visualRoot, new Vector3(0.55f, -0.17f, -1.18f), new Vector3(0.16f, 0.06f, 0.16f), new Color(1f, 0.5f, 0.18f));
        CreateHoverPart("FrontGlow", PrimitiveType.Sphere, visualRoot, new Vector3(0f, 0.02f, 1.38f), new Vector3(0.18f, 0.05f, 0.18f), new Color(0.28f, 0.95f, 1f));

        var visual = playerRoot.gameObject.AddComponent<HoverCarVisual>();
        visual.Configure(visualRoot);
    }

    private static void CreateHoverPart(string name, PrimitiveType type, Transform parent, Vector3 localPosition, Vector3 localScale, Color color)
    {
        GameObject part = GameObject.CreatePrimitive(type);
        part.name = name;
        part.transform.SetParent(parent, false);
        part.transform.localPosition = localPosition;
        part.transform.localRotation = Quaternion.identity;
        part.transform.localScale = localScale;

        Collider colliderRef = part.GetComponent<Collider>();
        if (colliderRef != null)
            Destroy(colliderRef);

        Renderer rendererRef = part.GetComponent<Renderer>();
        rendererRef.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        rendererRef.receiveShadows = false;
        rendererRef.sharedMaterial.color = color;
    }

    private static HudRefs EnsureHud(Transform root)
    {
        if (root == null)
            root = FindOrCreateRoot("HUD").transform;

        Canvas canvas = GetOrAddComponent<Canvas>(root.gameObject);
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        GetOrAddComponent<CanvasScaler>(root.gameObject);
        GetOrAddComponent<GraphicRaycaster>(root.gameObject);

        TextMeshProUGUI titleLabel = CreateOrGetLabel(root, "TitleLabel", new Vector2(28f, -22f), new Vector2(260f, 52f), 30f, TextAlignmentOptions.TopLeft);
        titleLabel.text = "GLITCH";
        titleLabel.color = Color.white;

        TextMeshProUGUI distanceLabel = CreateOrGetLabel(root, "DistanceLabel", new Vector2(28f, -78f), new Vector2(220f, 42f), 26f, TextAlignmentOptions.TopLeft);
        TextMeshProUGUI speedLabel = CreateOrGetLabel(root, "SpeedLabel", new Vector2(28f, -116f), new Vector2(220f, 38f), 22f, TextAlignmentOptions.TopLeft);
        TextMeshProUGUI scoreLabel = CreateOrGetLabel(root, "ScoreLabel", new Vector2(-180f, -28f), new Vector2(160f, 42f), 24f, TextAlignmentOptions.TopRight);
        TextMeshProUGUI statusLabel = CreateOrGetLabel(root, "StatusLabel", new Vector2(0f, -28f), new Vector2(280f, 34f), 18f, TextAlignmentOptions.Top);

        scoreLabel.rectTransform.anchorMin = new Vector2(1f, 1f);
        scoreLabel.rectTransform.anchorMax = new Vector2(1f, 1f);
        scoreLabel.rectTransform.pivot = new Vector2(1f, 1f);
        scoreLabel.text = "0";

        statusLabel.rectTransform.anchorMin = new Vector2(0.5f, 1f);
        statusLabel.rectTransform.anchorMax = new Vector2(0.5f, 1f);
        statusLabel.rectTransform.pivot = new Vector2(0.5f, 1f);
        statusLabel.color = new Color(0.5f, 0.95f, 1f);

        var panel = CreateOrGetPanel(root, "GameOverPanel", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(420f, 260f));
        var panelImage = panel.GetComponent<Image>();
        panelImage.color = new Color(0.05f, 0.08f, 0.12f, 0.88f);

        TextMeshProUGUI gameOverTitle = CreateOrGetLabel(panel.transform, "GameOverTitle", new Vector2(0f, -26f), new Vector2(340f, 48f), 28f, TextAlignmentOptions.Top);
        gameOverTitle.rectTransform.anchorMin = new Vector2(0.5f, 1f);
        gameOverTitle.rectTransform.anchorMax = new Vector2(0.5f, 1f);
        gameOverTitle.rectTransform.pivot = new Vector2(0.5f, 1f);

        TextMeshProUGUI gameOverStats = CreateOrGetLabel(panel.transform, "GameOverStats", new Vector2(30f, -82f), new Vector2(360f, 100f), 21f, TextAlignmentOptions.TopLeft);
        gameOverStats.textWrappingMode = TextWrappingModes.NoWrap;

        Button runAgainButton = CreateOrGetButton(panel.transform, "RunAgainButton", "Run Again", new Vector2(30f, 26f), new Color(0.08f, 0.62f, 0.92f));
        Button mainMenuButton = CreateOrGetButton(panel.transform, "MainMenuButton", "Main Menu", new Vector2(220f, 26f), new Color(0.18f, 0.2f, 0.28f));

        panel.SetActive(false);

        return new HudRefs(distanceLabel, speedLabel, scoreLabel, statusLabel, panel, gameOverTitle, gameOverStats, runAgainButton, mainMenuButton);
    }

    private static TextMeshProUGUI CreateOrGetLabel(Transform parent, string name, Vector2 anchoredPosition, Vector2 size, float fontSize, TextAlignmentOptions alignment)
    {
        Transform existing = parent.Find(name);
        GameObject labelObject = existing != null ? existing.gameObject : new GameObject(name);
        labelObject.transform.SetParent(parent, false);

        var rect = GetOrAddComponent<RectTransform>(labelObject);
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = size;

        var text = GetOrAddComponent<TextMeshProUGUI>(labelObject);
        text.fontSize = fontSize;
        text.alignment = alignment;
        text.color = Color.white;
        return text;
    }

    private static GameObject CreateOrGetPanel(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 size)
    {
        Transform existing = parent.Find(name);
        GameObject panelObject = existing != null ? existing.gameObject : new GameObject(name);
        panelObject.transform.SetParent(parent, false);

        var rect = GetOrAddComponent<RectTransform>(panelObject);
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = pivot;
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = size;

        GetOrAddComponent<Image>(panelObject);
        return panelObject;
    }

    private static Button CreateOrGetButton(Transform parent, string name, string label, Vector2 anchoredPosition, Color color)
    {
        Transform existing = parent.Find(name);
        GameObject buttonObject = existing != null ? existing.gameObject : new GameObject(name);
        buttonObject.transform.SetParent(parent, false);

        var rect = GetOrAddComponent<RectTransform>(buttonObject);
        rect.anchorMin = new Vector2(0f, 0f);
        rect.anchorMax = new Vector2(0f, 0f);
        rect.pivot = new Vector2(0f, 0f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = new Vector2(170f, 44f);

        var image = GetOrAddComponent<Image>(buttonObject);
        image.color = color;

        var button = GetOrAddComponent<Button>(buttonObject);
        button.targetGraphic = image;

        TextMeshProUGUI buttonLabel = CreateOrGetLabel(buttonObject.transform, "Label", new Vector2(0f, 0f), rect.sizeDelta, 22f, TextAlignmentOptions.Center);
        buttonLabel.rectTransform.anchorMin = Vector2.zero;
        buttonLabel.rectTransform.anchorMax = Vector2.one;
        buttonLabel.rectTransform.pivot = new Vector2(0.5f, 0.5f);
        buttonLabel.rectTransform.anchoredPosition = Vector2.zero;
        buttonLabel.text = label;
        buttonLabel.color = Color.white;

        return button;
    }

    private readonly struct HudRefs
    {
        public HudRefs(
            TextMeshProUGUI distanceLabel,
            TextMeshProUGUI speedLabel,
            TextMeshProUGUI scoreLabel,
            TextMeshProUGUI statusLabel,
            GameObject gameOverPanel,
            TextMeshProUGUI gameOverTitle,
            TextMeshProUGUI gameOverStats,
            Button runAgainButton,
            Button mainMenuButton)
        {
            DistanceLabel = distanceLabel;
            SpeedLabel = speedLabel;
            ScoreLabel = scoreLabel;
            StatusLabel = statusLabel;
            GameOverPanel = gameOverPanel;
            GameOverTitle = gameOverTitle;
            GameOverStats = gameOverStats;
            RunAgainButton = runAgainButton;
            MainMenuButton = mainMenuButton;
        }

        public TextMeshProUGUI DistanceLabel { get; }
        public TextMeshProUGUI SpeedLabel { get; }
        public TextMeshProUGUI ScoreLabel { get; }
        public TextMeshProUGUI StatusLabel { get; }
        public GameObject GameOverPanel { get; }
        public TextMeshProUGUI GameOverTitle { get; }
        public TextMeshProUGUI GameOverStats { get; }
        public Button RunAgainButton { get; }
        public Button MainMenuButton { get; }
    }
}
