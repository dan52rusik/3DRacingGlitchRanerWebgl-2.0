// ═══════════════════════════════════════════════════════════════
//  RoadPool.cs  —  Пул объектов одного биома
//  Использует Queue<T> — O(1) выдача и возврат.
//  Создаёт новые объекты при нехватке (авторасширение).
// ═══════════════════════════════════════════════════════════════
using System.Collections.Generic;
using UnityEngine;

public class RoadPool : MonoBehaviour
{
    // ── Приватные поля ─────────────────────────────────────────
    private Road            _prefab;
    private int             _biomeIndex;
    private Queue<Road>     _queue = new Queue<Road>(32);

    // ── Инициализация ──────────────────────────────────────────
    public void Initialize(Road prefab, int initialSize, int biomeIndex)
    {
        _prefab     = prefab;
        _biomeIndex = biomeIndex;

        for (int i = 0; i < initialSize; i++)
            _queue.Enqueue(CreateNew());
    }

    // ── Выдача объекта ─────────────────────────────────────────
    public Road Get()
    {
        Road road = _queue.Count > 0 ? _queue.Dequeue() : CreateNew();
        road.ResetState();
        return road;
    }

    // ── Возврат объекта ────────────────────────────────────────
    public void Return(Road road)
    {
        _queue.Enqueue(road);
    }

    // ── Приватные методы ───────────────────────────────────────
    private Road CreateNew()
    {
        Road road = Instantiate(_prefab, transform);
        road.Initialize(this, _biomeIndex);
        road.gameObject.SetActive(false);
        return road;
    }
}
