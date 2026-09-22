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
    public int RerollsUsed { get; private set; }
    public int RerollCost => (int)System.Math.Min(int.MaxValue, 30L + 15L * RerollsUsed);
    public int ClosedFrame { get; private set; } = -1;
    public bool BlocksInput => Choosing || ClosedFrame == Time.frameCount;
    public event System.Action Changed;
    private readonly System.Collections.Generic.List<Choice> catalog =
        new System.Collections.Generic.List<Choice>();

    public RunUpgradeState()
    {
        Add("Arsenal reforzado", "+6% daño de todas las torres", "damage", "all", .06f, 20);
        Add("Mecanismos ágiles", "+6% cadencia de todas las torres", "rate", "all", .06f, 20);
        Add("Vigilancia", "+5% alcance de todas las torres", "range", "all", .05f, 20);
        Add("Botín de caza", "+10% oro por enemigo eliminado", "gold", "all", .10f, 15);
        Add("Construcción eficiente", "−5% coste de todas las torres", "discount", "all", .05f, 6);
        Add("Presagio certero", "+4% probabilidad de golpe crítico", "crit_chance", "all", .04f, 10);
        Add("Golpe despiadado", "+20% daño de los golpes críticos", "crit_damage", "all", .20f, 8);
        string[] families = { "basic", "cannon", "lightning", "flame", "arcane" };
        string[] names = { "Básica", "Cañón", "Rayos", "Fuego", "Arcana" };
        for (int i = 0; i < families.Length; i++)
        {
            Add(names[i] + ": potencia", "+10% daño de " + names[i], "damage", families[i], .10f, 15);
            Add(names[i] + ": ritmo", "+8% cadencia de " + names[i], "rate", families[i], .08f, 15);
            Add(names[i] + ": dominio", "+7% alcance de " + names[i], "range", families[i], .07f, 10);
            Add(names[i] + ": precisión", "+5% crítico de " + names[i], "crit_chance", families[i], .05f, 8);
            Add(names[i] + ": planos", "−8% coste de " + names[i], "discount", families[i], .08f, 5);
        }
        Add("Onda expansiva", "+10% radio de explosión del cañón", "area", "cannon", .10f, 10);
        Add("Metralla maldita", "+12% daño de la explosión del cañón", "splash", "cannon", .12f, 12);
        Add("Lenguas de fuego", "+8% alcance de fuego (detección y daño)", "range", "flame", .08f, 10);
        Add("Abanico ardiente", "+10% apertura del cono de fuego", "area", "flame", .10f, 10);
        Add("Brasas persistentes", "Quema por 20% del daño por segundo", "burn", "flame", .20f, 15);
        Add("Fuego inextinguible", "+0,75 s de duración de quemadura", "burn_duration", "flame", .75f, 8);
        Add("Arco adicional", "+1 rebote de rayos", "bounces", "lightning", 1f, 6);
        Add("Conducción", "+10% distancia entre rebotes", "chain", "lightning", .10f, 10);
    }

    private void Add(string title, string description, string stat, string family, float amount, int limit = 3)
    {
        catalog.Add(new Choice { Title = title, Description = description, Stat = stat,
            Family = family, Amount = amount, Limit = limit });
    }

    public Choice[] Draw(Choice[] excluded = null)
    {
        var available = catalog.FindAll(choice => choice.Stacks < choice.Limit &&
            (excluded == null || System.Array.IndexOf(excluded, choice) < 0));
        int freshCount = available.Count;
        // Near exhaustion keep three options when possible, but prefer fresh ones.
        if (available.Count < 3 && excluded != null)
            foreach (Choice oldChoice in excluded)
                if (catalog.Contains(oldChoice) && oldChoice.Stacks < oldChoice.Limit && !available.Contains(oldChoice))
                    available.Add(oldChoice);
        int count = Mathf.Min(3, available.Count);
        var result = new Choice[count];
        for (int i = 0; i < count; i++)
        {
            int index = Random.Range(0, i == 0 && freshCount > 0 ? freshCount : available.Count);
            result[i] = available[index];
            available.RemoveAt(index);
        }
        return result;
    }

    public bool CanReroll(Choice[] previous) => Choosing && previous != null &&
        catalog.Exists(choice => choice.Stacks < choice.Limit && System.Array.IndexOf(previous, choice) < 0);

    public bool TryReroll(Choice[] previous, System.Func<int, bool> spendGold, out Choice[] replacement)
    {
        replacement = null;
        if (!CanReroll(previous) || spendGold == null) return false;
        // Draw before paying, and never charge for an unchanged set of choices.
        Choice[] next = Draw(previous);
        if (!System.Array.Exists(next, choice => System.Array.IndexOf(previous, choice) < 0)) return false;
        if (!spendGold(RerollCost)) return false;
        RerollsUsed++;
        replacement = next;
        return true;
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
    public float RollDamage(float damage, TowerAttackData attack)
    {
        return ResolveDamage(damage, attack, Random.value);
    }

    public float ResolveDamage(float damage, TowerAttackData attack, float roll)
    {
        float criticalChance = Mathf.Min(0.75f, Mathf.Clamp01(Bonus("crit_chance", attack)));
        if (roll >= criticalChance) return damage;
        return damage * (1.75f + Bonus("crit_damage", attack));
    }

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
