using UnityEngine;
using UnityEngine.SceneManagement;

namespace GlitchRacer
{
    public static class GlitchRacerRuntimeBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void RegisterSceneCallbacks()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void EnsureBootstrap()
        {
            if (Object.FindFirstObjectByType<GlitchRacerGame>() != null)
            {
                return;
            }

            BuildIntoCurrentScene(false);
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            EnsureBootstrap();
        }

        public static GlitchRacerGame BuildIntoCurrentScene(bool clearScene)
        {
#if UNITY_EDITOR
            if (clearScene)
            {
                GameObject[] sceneObjects = Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None);
                for (int i = 0; i < sceneObjects.Length; i++)
                {
                    if (sceneObjects[i].transform.parent == null)
                    {
                        Object.DestroyImmediate(sceneObjects[i]);
                    }
                }
            }
#endif

            GameObject root = new("GlitchRacerBootstrap");
            GlitchRacerGame game = root.AddComponent<GlitchRacerGame>();
            TrackSegmentSpawner spawner = root.AddComponent<TrackSegmentSpawner>();
            GlitchRacerHud hud = root.AddComponent<GlitchRacerHud>();

            Camera camera = Camera.main;
            if (camera == null)
            {
                camera = Object.FindFirstObjectByType<Camera>();
            }

            if (camera == null)
            {
                camera = new GameObject("Main Camera").AddComponent<Camera>();
            }

            camera.tag = "MainCamera";
            if (camera.GetComponent<AudioListener>() == null)
            {
                camera.gameObject.AddComponent<AudioListener>();
            }

            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.fieldOfView = 74f;

            GlitchCameraRig rig = camera.GetComponent<GlitchCameraRig>();
            if (rig == null)
            {
                rig = camera.gameObject.AddComponent<GlitchCameraRig>();
            }

            if (Object.FindFirstObjectByType<Light>() == null)
            {
                GameObject lightObject = new("Directional Light");
                Light light = lightObject.AddComponent<Light>();
                light.type = LightType.Directional;
                light.intensity = 0.92f;
                light.color = new Color(0.45f, 0.68f, 1f);
                light.transform.rotation = Quaternion.Euler(24f, -32f, 0f);
            }

            RunnerPlayer player = CreatePlayer();

            spawner.Configure(game);
            rig.Configure(game, player.transform);
            hud.Configure(game);
            player.Configure(game);
            player.GetComponent<VirusCarEffects>()?.Configure(game);
            game.Configure(player, spawner, rig, hud);

            return game;
        }

        private static RunnerPlayer CreatePlayer()
        {
            GameObject playerRoot = new("VirusCar");
            playerRoot.transform.position = new Vector3(0f, 0.95f, 0f);
            playerRoot.transform.localScale = Vector3.one;

            Rigidbody body = playerRoot.AddComponent<Rigidbody>();
            body.isKinematic = true;
            body.useGravity = false;

            BoxCollider collider = playerRoot.AddComponent<BoxCollider>();
            collider.size = new Vector3(1f, 0.8f, 2f);
            collider.center = new Vector3(0f, 0.05f, 0f);

            Transform visualRoot = HoverCarFactory.RebuildVisual(playerRoot.transform, "VirusCarVisual");
            HoverCarVisual visual = playerRoot.AddComponent<HoverCarVisual>();
            visual.Configure(visualRoot);

            RunnerPlayer runnerPlayer = playerRoot.AddComponent<RunnerPlayer>();
            playerRoot.AddComponent<VirusCarEffects>();
            return runnerPlayer;
        }
    }
}
