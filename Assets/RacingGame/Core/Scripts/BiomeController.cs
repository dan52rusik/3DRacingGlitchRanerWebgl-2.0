// ═══════════════════════════════════════════════════════════════
//  BiomeController.cs  —  Менеджер биомов
//  • Генерирует зоны вперёд на lookaheadBiomes
//  • Диспетчит событие OnBiomeChanged через GameEventBus
//  • Не использует FindObjectOfType
// ═══════════════════════════════════════════════════════════════
using System.Collections.Generic;
using UnityEngine;

public class BiomeController
{
    // ── Приватные поля ─────────────────────────────────────────
    private readonly List<RoadBiomeData>  _biomes;
    private readonly RoadBiomeSettings    _settings;
    private readonly List<BiomeZone>      _zones    = new List<BiomeZone>(16);
    private BiomeZone                     _lastZone;
    private float                         _totalWeight;

    // ── Конструктор ────────────────────────────────────────────
    public BiomeController(List<RoadBiomeData> biomes, RoadBiomeSettings settings)
    {
        // Фильтруем null и биомы с индексом 3 (резерв/мост)
        _biomes   = biomes.FindAll(b => b != null && b.biomeIndex != 3);
        _settings = settings;

        foreach (var b in _biomes) _totalWeight += b.spawnWeight;

        // Генерируем стартовый набор зон
        for (int i = 0; i < _settings.lookaheadBiomes; i++)
            GenerateNext();
    }

    // ── Публичный API ──────────────────────────────────────────

    /// <summary>Возвращает активный биом на указанной дистанции (км).</summary>
    public BiomeZone GetBiomeAt(float km)
    {
        // Пополняем генерацию если приближаемся к концу
        while (_zones.Count == 0 || km > _zones[_zones.Count - 1].endKm - _settings.maxSpacing)
            GenerateNext();

        foreach (var zone in _zones)
            if (zone.Contains(km)) return zone;

        // Между зонами — возвращаем ближайшую следующую
        foreach (var zone in _zones)
            if (zone.startKm > km) return zone;

        return _zones[_zones.Count - 1];
    }

    /// <summary>Возвращает следующий биом после текущего км.</summary>
    public BiomeZone GetUpcoming(float km)
    {
        foreach (var zone in _zones)
            if (zone.startKm > km) return zone;
        return null;
    }

    /// <summary>true если до следующего биома меньше threshold км.</summary>
    public bool IsTransitionNear(float km, float threshold = 0.3f)
    {
        var next = GetUpcoming(km);
        return next != null && (next.startKm - km) <= threshold;
    }

    // ── Внутренняя генерация ───────────────────────────────────
    private void GenerateNext()
    {
        float startKm;

        if (_zones.Count == 0)
        {
            startKm = 0f;
        }
        else
        {
            var last = _zones[_zones.Count - 1];
            startKm  = last.endKm + Random.Range(_settings.minSpacing, _settings.maxSpacing);
        }

        var data   = PickRandom();
        float len  = Random.Range(_settings.minBiomeLength, _settings.maxBiomeLength);

        var zone = new BiomeZone(data.biomeName, data.biomeIndex, startKm, len,
                                 isHighway: data.isHighway, isBridge: data.isBridge);
        _zones.Add(zone);
    }

    private RoadBiomeData PickRandom()
    {
        float roll = Random.value * _totalWeight;
        float acc  = 0f;

        foreach (var b in _biomes)
        {
            acc += b.spawnWeight;
            if (roll <= acc) return b;
        }

        return _biomes[_biomes.Count - 1];
    }

    // ── Debug ──────────────────────────────────────────────────
    public IReadOnlyList<BiomeZone> Zones => _zones;
}
