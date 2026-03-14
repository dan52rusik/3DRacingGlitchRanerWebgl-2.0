// ═══════════════════════════════════════════════════════════════
//  BiomeVisualTransition.cs  —  Плавный переход визуала биомов
//
//  • Плавно меняет цвет неба (Skybox material tint) и тумана
//  • Слушает GameEventBus.OnBiomeChanged
//  • Работает только через RenderSettings — без PostProcessing
//    (совместимо с WebGL URP/Built-in)
// ═══════════════════════════════════════════════════════════════
using System.Collections;
using UnityEngine;

public class BiomeVisualTransition : MonoBehaviour
{
    [Header("Skybox материал (опционально)")]
    [SerializeField] private Material skyboxMaterial;

    [Header("Время перехода (сек)")]
    [SerializeField] private float transitionDuration = 3f;

    // ID шейдерных свойств
    private static readonly int TopColorID     = Shader.PropertyToID("_TopColor");
    private static readonly int BotColorID     = Shader.PropertyToID("_BottomColor");

    // Текущие целевые значения
    private Color _targetFog;
    private Color _targetSkyTop;
    private Color _targetSkyHor;

    // Источники биомов для поиска данных
    private RoadBiomeData[] _allBiomes;

    private Coroutine _transitionRoutine;

    void Awake()
    {
        _allBiomes = Resources.FindObjectsOfTypeAll<RoadBiomeData>();
        GameEventBus.OnBiomeChanged += OnBiomeChanged;
    }

    void OnDestroy()
    {
        GameEventBus.OnBiomeChanged -= OnBiomeChanged;
    }

    public void Configure(Material material, float duration)
    {
        skyboxMaterial = material;
        transitionDuration = duration;
    }

    // ── Обработчик ────────────────────────────────────────────
    private void OnBiomeChanged(BiomeZone zone)
    {
        var data = FindBiomeData(zone.biomeIndex);
        if (data == null) return;

        if (_transitionRoutine != null) StopCoroutine(_transitionRoutine);
        _transitionRoutine = StartCoroutine(DoTransition(data));
    }

    // ── Корутина перехода ─────────────────────────────────────
    private IEnumerator DoTransition(RoadBiomeData data)
    {
        Color startFog    = RenderSettings.fogColor;
        Color startSkyTop = skyboxMaterial != null
            ? skyboxMaterial.GetColor(TopColorID) : Color.black;
        Color startSkyHor = skyboxMaterial != null
            ? skyboxMaterial.GetColor(BotColorID) : Color.black;
        float startFogEnd = RenderSettings.fogEndDistance;

        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / transitionDuration;
            float smooth = Mathf.SmoothStep(0f, 1f, t);

            // Туман
            RenderSettings.fogColor       = Color.Lerp(startFog, data.fogColor, smooth);
            RenderSettings.fogEndDistance = Mathf.Lerp(startFogEnd, data.fogDensityStart, smooth);

            // Скайбокс (только если задан материал)
            if (skyboxMaterial != null)
            {
                skyboxMaterial.SetColor(TopColorID, Color.Lerp(startSkyTop, data.skyColorTop,     smooth));
                skyboxMaterial.SetColor(BotColorID, Color.Lerp(startSkyHor, data.skyColorHorizon, smooth));
                DynamicGI.UpdateEnvironment();
            }

            yield return null;
        }
    }

    // ── Вспомогательный поиск данных биома ────────────────────
    private RoadBiomeData FindBiomeData(int index)
    {
        foreach (var b in _allBiomes)
            if (b != null && b.biomeIndex == index) return b;
        return null;
    }
}
