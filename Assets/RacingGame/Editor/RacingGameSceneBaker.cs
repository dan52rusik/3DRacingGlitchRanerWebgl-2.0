using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class RacingGameSceneBaker
{
    private const string RootFolder = "Assets/RacingGame/Generated";
    private const string PrefabsFolder = RootFolder + "/Prefabs";
    private const string BiomesFolder = RootFolder + "/Biomes";
    private const string SettingsFolder = RootFolder + "/Settings";

    [MenuItem("Tools/RacingGame/Bake Scene Setup")]
    public static void BakeSceneSetup()
    {
        EnsureFolders();

        GameObject roadPrefab = CreateOrUpdateRoadPrefab();
        RoadBiomeSettings settings = CreateOrUpdateSettings();
        var biomes = CreateOrUpdateBiomes(roadPrefab);

        Camera camera = EnsureCamera();
        EnsureDirectionalLight();
        Transform deletionAnchor = EnsureDeletionAnchor(camera.transform);

        GameObject systems = GetOrCreateRoot("GameSystems");
        GameObject hud = GetOrCreateRoot("HUD");

        var roadManager = GetOrAddComponent<RoadManager>(systems);
        var difficultyManager = GetOrAddComponent<DifficultyManager>(systems);
        var obstacleSpawner = GetOrAddComponent<ObstacleSpawner>(systems);
        var biomeVisual = GetOrAddComponent<BiomeVisualTransition>(systems);
        var leaderboard = GetOrAddComponent<YandexLeaderboard>(systems);

        ConfigureRoadManager(roadManager, biomes, settings, deletionAnchor);
        ConfigureObstacleSpawner(obstacleSpawner, biomes);
        ConfigureBiomeVisual(biomeVisual, RenderSettings.skybox, settings.biomeTransitionTime);

        var labels = EnsureHud(hud);
        var distanceTracker = GetOrAddComponent<DistanceTracker>(hud);
        ConfigureDistanceTracker(distanceTracker, labels.distance, labels.speed);

        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.Linear;
        RenderSettings.fogEndDistance = 140f;

        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        AssetDatabase.SaveAssets();
        EditorSceneManager.SaveOpenScenes();

        Debug.Log("RacingGame scene baked. Objects and assets are now stored in the scene/project.");
    }

    private static void EnsureFolders()
    {
        CreateFolderIfMissing("Assets/RacingGame", "Generated");
        CreateFolderIfMissing(RootFolder, "Prefabs");
        CreateFolderIfMissing(RootFolder, "Biomes");
        CreateFolderIfMissing(RootFolder, "Settings");
    }

    private static void CreateFolderIfMissing(string parent, string name)
    {
        string path = parent + "/" + name;
        if (!AssetDatabase.IsValidFolder(path))
            AssetDatabase.CreateFolder(parent, name);
    }

    private static GameObject CreateOrUpdateRoadPrefab()
    {
        const string prefabPath = PrefabsFolder + "/RoadSegment.prefab";
        GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);

        var temp = GameObject.CreatePrimitive(PrimitiveType.Cube);
        temp.name = "RoadSegment";
        temp.transform.localScale = new Vector3(8f, 0.5f, 24f);
        temp.transform.position = new Vector3(0f, -0.25f, 0f);
        GetOrAddComponent<Road>(temp);
        GetOrAddComponent<PlatformMovement>(temp);

        var renderer = temp.GetComponent<Renderer>();
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = false;

        GameObject prefab = existing == null
            ? PrefabUtility.SaveAsPrefabAsset(temp, prefabPath)
            : PrefabUtility.SaveAsPrefabAssetAndConnect(temp, prefabPath, InteractionMode.AutomatedAction);

        Object.DestroyImmediate(temp);
        return prefab;
    }

    private static RoadBiomeSettings CreateOrUpdateSettings()
    {
        const string path = SettingsFolder + "/BiomeSettings.asset";
        var settings = AssetDatabase.LoadAssetAtPath<RoadBiomeSettings>(path);
        if (settings == null)
        {
            settings = ScriptableObject.CreateInstance<RoadBiomeSettings>();
            AssetDatabase.CreateAsset(settings, path);
        }

        settings.defaultBiomeLength = 2.5f;
        settings.minBiomeLength = 0.8f;
        settings.maxBiomeLength = 1.8f;
        settings.minSpacing = 0f;
        settings.maxSpacing = 0.4f;
        settings.biomeTransitionTime = 2.5f;
        settings.lookaheadBiomes = 5;
        EditorUtility.SetDirty(settings);
        return settings;
    }

    private static List<RoadBiomeData> CreateOrUpdateBiomes(GameObject roadPrefab)
    {
        return new List<RoadBiomeData>
        {
            CreateOrUpdateBiome("Biome_City.asset", 0, "City", roadPrefab, new Color(0.18f, 0.45f, 0.95f), new Color(0.85f, 0.92f, 1f), new Color(0.55f, 0.62f, 0.75f), 0.45f),
            CreateOrUpdateBiome("Biome_Sunset.asset", 1, "Sunset", roadPrefab, new Color(0.96f, 0.48f, 0.22f), new Color(1f, 0.84f, 0.55f), new Color(0.82f, 0.56f, 0.42f), 0.35f),
            CreateOrUpdateBiome("Biome_Night.asset", 2, "Night", roadPrefab, new Color(0.05f, 0.08f, 0.2f), new Color(0.18f, 0.24f, 0.38f), new Color(0.12f, 0.16f, 0.25f), 0.2f),
        };
    }

    private static RoadBiomeData CreateOrUpdateBiome(string fileName, int biomeIndex, string biomeName, GameObject roadPrefab, Color top, Color horizon, Color fog, float weight)
    {
        string path = BiomesFolder + "/" + fileName;
        var biome = AssetDatabase.LoadAssetAtPath<RoadBiomeData>(path);
        if (biome == null)
        {
            biome = ScriptableObject.CreateInstance<RoadBiomeData>();
            AssetDatabase.CreateAsset(biome, path);
        }

        biome.biomeIndex = biomeIndex;
        biome.biomeName = biomeName;
        biome.roadPrefab = roadPrefab;
        biome.skyColorTop = top;
        biome.skyColorHorizon = horizon;
        biome.fogColor = fog;
        biome.fogDensityStart = 140f;
        biome.spawnWeight = weight;
        biome.obstacleSets = new ObstacleSetData[0];
        EditorUtility.SetDirty(biome);
        return biome;
    }

    private static Camera EnsureCamera()
    {
        Camera camera = Camera.main;
        if (camera == null)
        {
            var cameraObject = new GameObject("Main Camera");
            cameraObject.tag = "MainCamera";
            camera = cameraObject.AddComponent<Camera>();
        }

        camera.clearFlags = CameraClearFlags.Skybox;
        camera.fieldOfView = 60f;
        camera.transform.position = new Vector3(0f, 7f, -18f);
        camera.transform.rotation = Quaternion.Euler(12f, 0f, 0f);
        return camera;
    }

    private static void EnsureDirectionalLight()
    {
        if (Object.FindFirstObjectByType<Light>() != null) return;

        var lightObject = new GameObject("Directional Light");
        var light = lightObject.AddComponent<Light>();
        light.type = LightType.Directional;
        light.intensity = 1.1f;
        light.transform.rotation = Quaternion.Euler(35f, -25f, 0f);
    }

    private static Transform EnsureDeletionAnchor(Transform cameraTransform)
    {
        Transform anchor = cameraTransform.Find("DeletionAnchor");
        if (anchor != null) return anchor;

        anchor = new GameObject("DeletionAnchor").transform;
        anchor.SetParent(cameraTransform, false);
        anchor.localPosition = Vector3.zero;
        anchor.localRotation = Quaternion.identity;
        return anchor;
    }

    private static GameObject GetOrCreateRoot(string name)
    {
        GameObject root = GameObject.Find(name);
        if (root == null)
            root = new GameObject(name);
        return root;
    }

    private static T GetOrAddComponent<T>(GameObject target) where T : Component
    {
        T component = target.GetComponent<T>();
        if (component == null)
            component = Undo.AddComponent<T>(target);
        return component;
    }

    private static void ConfigureRoadManager(RoadManager manager, List<RoadBiomeData> biomes, RoadBiomeSettings settings, Transform deletionAnchor)
    {
        var serializedObject = new SerializedObject(manager);
        SetObjectReferenceArray(serializedObject.FindProperty("biomes"), biomes);
        serializedObject.FindProperty("biomeSettings").objectReferenceValue = settings;
        serializedObject.FindProperty("deletionAnchor").objectReferenceValue = deletionAnchor;
        serializedObject.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(manager);
    }

    private static void ConfigureObstacleSpawner(ObstacleSpawner spawner, List<RoadBiomeData> biomes)
    {
        var serializedObject = new SerializedObject(spawner);
        SetObjectReferenceArray(serializedObject.FindProperty("biomes"), biomes);
        serializedObject.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(spawner);
    }

    private static void ConfigureBiomeVisual(BiomeVisualTransition visual, Material skybox, float duration)
    {
        var serializedObject = new SerializedObject(visual);
        serializedObject.FindProperty("skyboxMaterial").objectReferenceValue = skybox;
        serializedObject.FindProperty("transitionDuration").floatValue = duration;
        serializedObject.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(visual);
    }

    private static (TextMeshProUGUI distance, TextMeshProUGUI speed) EnsureHud(GameObject hudRoot)
    {
        Canvas canvas = GetOrAddComponent<Canvas>(hudRoot);
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        GetOrAddComponent<CanvasScaler>(hudRoot);
        GetOrAddComponent<GraphicRaycaster>(hudRoot);

        var distance = GetOrCreateLabel(hudRoot.transform, "DistanceLabel", new Vector2(20f, -20f), 42f);
        var speed = GetOrCreateLabel(hudRoot.transform, "SpeedLabel", new Vector2(20f, -68f), 30f);
        return (distance, speed);
    }

    private static TextMeshProUGUI GetOrCreateLabel(Transform parent, string name, Vector2 anchoredPosition, float fontSize)
    {
        Transform existing = parent.Find(name);
        GameObject labelObject = existing != null ? existing.gameObject : new GameObject(name);
        labelObject.transform.SetParent(parent, false);

        var rect = GetOrAddComponent<RectTransform>(labelObject);
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = new Vector2(420f, 48f);

        var text = GetOrAddComponent<TextMeshProUGUI>(labelObject);
        text.fontSize = fontSize;
        text.alignment = TextAlignmentOptions.TopLeft;
        text.color = Color.white;
        text.text = "0";
        return text;
    }

    private static void ConfigureDistanceTracker(DistanceTracker tracker, TextMeshProUGUI distanceLabel, TextMeshProUGUI speedLabel)
    {
        var serializedObject = new SerializedObject(tracker);
        serializedObject.FindProperty("distanceLabel").objectReferenceValue = distanceLabel;
        serializedObject.FindProperty("speedLabel").objectReferenceValue = speedLabel;
        serializedObject.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(tracker);
    }

    private static void SetObjectReferenceArray(SerializedProperty property, List<RoadBiomeData> values)
    {
        property.arraySize = values.Count;
        for (int i = 0; i < values.Count; i++)
            property.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
    }
}
