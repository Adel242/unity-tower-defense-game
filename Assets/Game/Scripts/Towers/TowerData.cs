using UnityEngine;

[CreateAssetMenu(
    fileName = "NewTowerData",
    menuName = "Tower Defense/Tower Data"
)]
public class TowerData : ScriptableObject
{
    public string towerName;

    public float damage = 5f;
    public float range = 8f;
    public float fireRate = 2f;

    public float rotationSpeed = 250f;
    public float searchRotationSpeed = 100f;
    public float aimTolerance = 20f;

    public float firstShotDelay = 0.25f;
    public int cost = 100;

    [SerializeField] private TowerAttackData attackData;

    public TowerAttackData AttackData => attackData;
    public float Damage => damage * RunUpgradeState.Current.Multiplier("damage", attackData);
    public float FireRate => fireRate * RunUpgradeState.Current.Multiplier("rate", attackData);
    public float Range => range * RunUpgradeState.Current.Multiplier("range", attackData);
    public int Cost => Mathf.Max(1, Mathf.CeilToInt(cost *
        Mathf.Max(0.5f, 1f - RunUpgradeState.Current.Bonus("discount", attackData))));
}

// Session-only modifiers. WaveManager owns reset; ScriptableObjects stay unchanged.
public sealed class RunUpgradeState
{
    public static RunUpgradeState Current { get; private set; } = new RunUpgradeState();
    public static void Reset() { Current = new RunUpgradeState(); }
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetDomain() { Reset(); }

    public sealed class Choice
    {
        public string Title, Description, Stat, Family;
        public float Amount;
        public int Stacks;
        public int Limit = 3;
    }

    public bool Choosing { get; set; }
    public int ClosedFrame { get; private set; } = -1;
    public bool BlocksInput => Choosing || ClosedFrame == Time.frameCount;
    public event System.Action Changed;
    private readonly System.Collections.Generic.List<Choice> catalog =
        new System.Collections.Generic.List<Choice>();

    public RunUpgradeState()
    {
        Add("Arsenal reforzado", "+6% daño de todas las torres", "damage", "all", .06f);
        Add("Mecanismos ágiles", "+6% cadencia de todas las torres", "rate", "all", .06f);
        Add("Vigilancia", "+5% alcance de todas las torres", "range", "all", .05f);
        Add("Botín de caza", "+10% oro por enemigo eliminado", "gold", "all", .10f);
        Add("Construcción eficiente", "−5% coste de todas las torres", "discount", "all", .05f);
        string[] families = { "basic", "cannon", "lightning", "flame", "arcane" };
        string[] names = { "Básica", "Cañón", "Rayos", "Fuego", "Arcana" };
        for (int i = 0; i < families.Length; i++)
        {
            Add(names[i] + ": potencia", "+10% daño de " + names[i], "damage", families[i], .10f);
            Add(names[i] + ": ritmo", "+8% cadencia de " + names[i], "rate", families[i], .08f);
            Add(names[i] + ": planos", "−8% coste de " + names[i], "discount", families[i], .08f);
        }
        Add("Onda expansiva", "+10% radio de explosión del cañón", "area", "cannon", .10f);
        Add("Lenguas de fuego", "+8% alcance de fuego (detección y daño)", "range", "flame", .08f);
        Add("Abanico ardiente", "+10% apertura del cono de fuego", "area", "flame", .10f);
        Add("Arco adicional", "+1 rebote de rayos", "bounces", "lightning", 1f, 2);
        Add("Conducción", "+10% distancia entre rebotes", "chain", "lightning", .10f);
    }

    private void Add(string title, string description, string stat, string family, float amount, int limit = 3)
    {
        catalog.Add(new Choice { Title = title, Description = description, Stat = stat,
            Family = family, Amount = amount, Limit = limit });
    }

    public Choice[] Draw()
    {
        var available = catalog.FindAll(choice => choice.Stacks < choice.Limit);
        int count = Mathf.Min(3, available.Count);
        var result = new Choice[count];
        for (int i = 0; i < count; i++)
        {
            int index = Random.Range(0, available.Count);
            result[i] = available[index];
            available.RemoveAt(index);
        }
        return result;
    }

    public void Apply(Choice choice)
    {
        if (!Choosing || choice == null || !catalog.Contains(choice) || choice.Stacks >= choice.Limit) return;
        choice.Stacks++;
        Choosing = false;
        ClosedFrame = Time.frameCount;
        Changed?.Invoke();
    }

    public float Multiplier(string stat, TowerAttackData attack) => 1f + Bonus(stat, attack);
    public float Bonus(string stat, TowerAttackData attack = null)
    {
        string family = attack is CannonAttackData ? "cannon" : attack is LightningAttackData ? "lightning" :
            attack is FlameAttackData ? "flame" : attack is ArcaneAttackData ? "arcane" : "basic";
        float value = 0f;
        foreach (Choice choice in catalog)
            if (choice.Stat == stat && (choice.Family == "all" || choice.Family == family))
                value += choice.Amount * choice.Stacks;
        return value;
    }
}
