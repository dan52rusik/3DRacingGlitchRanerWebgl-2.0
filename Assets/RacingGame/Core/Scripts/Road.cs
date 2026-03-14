// ═══════════════════════════════════════════════════════════════
//  Road.cs  —  Компонент одного сегмента дороги
//  Хранит длину, биом, ссылку на пул. Умеет возвращаться в пул.
// ═══════════════════════════════════════════════════════════════
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class Road : MonoBehaviour
{
    // ── Публичные свойства ─────────────────────────────────────
    public int   BiomeIndex  { get; private set; }
    public float Length      { get; private set; }

    // ── Приватные поля ─────────────────────────────────────────
    private RoadPool _pool;
    private bool     _recycled;

    // ── Инициализация ──────────────────────────────────────────
    /// <summary>Вызывается RoadPool при создании или выдаче объекта.</summary>
    public void Initialize(RoadPool pool, int biomeIndex)
    {
        _pool       = pool;
        BiomeIndex  = biomeIndex;
        _recycled   = false;

        // Кэшируем длину один раз по bounds рендерера
        if (Length <= 0f)
        {
            var r = GetComponentInChildren<Renderer>();
            Length = r != null ? r.bounds.size.z : 21f;
        }
    }

#if UNITY_EDITOR
    public void ConfigureForBake(int biomeIndex, float length)
    {
        BiomeIndex = biomeIndex;
        Length = length;
    }
#endif

    // ── Возврат в пул ──────────────────────────────────────────
    public void Recycle()
    {
        if (_recycled) return;
        _recycled = true;
        gameObject.SetActive(false);
        _pool.Return(this);
    }

    // ── Сброс при выдаче из пула ───────────────────────────────
    /// <summary>Вызывается RoadPool.Get() перед выдачей.</summary>
    public void ResetState()
    {
        _recycled = false;
        // Здесь можно сбрасывать анимации, частицы, препятствия и т.д.
    }
}
