// ═══════════════════════════════════════════════════════════════
//  BiomeZone.cs  —  Данные одной активной зоны биома
// ═══════════════════════════════════════════════════════════════
public class BiomeZone
{
    public string name;
    public int    biomeIndex;
    public float  startKm;
    public float  endKm;
    public bool   isHighway;
    public bool   isBridge;

    public BiomeZone(string name, int index, float startKm, float lengthKm,
                     bool isHighway = false, bool isBridge = false)
    {
        this.name       = name;
        this.biomeIndex = index;
        this.startKm    = startKm;
        this.endKm      = startKm + lengthKm;
        this.isHighway  = isHighway;
        this.isBridge   = isBridge;
    }

    public bool Contains(float km) => km >= startKm && km < endKm;
}
