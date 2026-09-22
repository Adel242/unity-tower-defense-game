// Lightweight deterministic rule tests against the production TowerData.cs.
// Engine stubs are test-only; these do not validate Unity UI or lifecycle.
using System;
using System.Linq;
using System.Collections.Generic;

static class Program
{
    static void Check(bool condition, string name)
    {
        if (!condition) throw new Exception(name);
        Console.WriteLine("PASS: " + name);
    }
    static RunUpgradeState.Choice Find(RunUpgradeState state, string title)
    {
        for (int i = 0; i < 10000; i++)
        {
            var match = state.Draw().FirstOrDefault(c => c.Title == title);
            if (match != null) return match;
        }
        throw new Exception("Missing choice: " + title);
    }
    static void Pick(RunUpgradeState state, RunUpgradeState.Choice choice)
    {
        state.Choosing = true;
        state.Apply(choice);
    }
    static void Main()
    {
        RunUpgradeState.Reset();
        var state = RunUpgradeState.Current;
        var seen = new HashSet<string>();
        for (int i = 0; i < 500; i++)
        {
            var draw = state.Draw();
            if (draw.Length != 3 || draw.Select(c => c.Title).Distinct().Count() != 3)
                throw new Exception("Duplicate or missing cards");
            foreach (var c in draw) seen.Add(c.Title);
        }
        Check(seen.Count == 40, "40 choices, 500 offers without duplicate cards");
        var data = new TowerData { damage = 10, fireRate = 2, range = 6, cost = 100 };
        var damage = Find(state, "Arsenal reforzado");
        state.Apply(damage);
        Check(damage.Stacks == 0, "No selection outside draft");
        Pick(state, damage);
        state.Apply(damage);
        Check(damage.Stacks == 1 && Math.Abs(data.Damage - 10.6f) < .001f,
            "Exactly one choice per draft; effective damage");
        Check(data.damage == 10 && data.cost == 100, "Base asset values unchanged");
        for (int i = 0; i < damage.Limit + 2; i++) Pick(state, damage);
        Check(damage.Stacks == 20, "Extended stack limit enforced");
        Check(Enumerable.Range(0, 500).All(_ => !state.Draw().Contains(damage)), "Capped upgrade excluded");
        var rate = Find(state, "Mecanismos ágiles"); Pick(state, rate);
        Check(Math.Abs(data.FireRate - 2.12f) < .001f, "Fire rate modifier");
        var area = Find(state, "Onda expansiva"); Pick(state, area);
        Check(Math.Abs(state.Multiplier("area", new CannonAttackData()) - 1.1f) < .001f &&
            state.Multiplier("area", new FlameAttackData()) == 1, "Family-specific area");
        var bounce = Find(state, "Arco adicional"); Pick(state, bounce); Pick(state, bounce); Pick(state, bounce);
        Check(state.Bonus("bounces", new LightningAttackData()) == 3, "Rebound stacks");
        var critical = Find(state, "Presagio certero"); Pick(state, critical);
        Check(Math.Abs(state.ResolveDamage(100, new ArcaneAttackData(), .02f) - 175f) < .001f &&
            Math.Abs(state.ResolveDamage(100, new ArcaneAttackData(), .9f) - 100f) < .001f,
            "Critical chance resolves once from a supplied roll");
        var burn = Find(state, "Brasas persistentes"); Pick(state, burn);
        Check(Math.Abs(state.Bonus("burn", new FlameAttackData()) - .2f) < .001f &&
            state.Bonus("burn", new CannonAttackData()) == 0f, "Burn remains flame-specific");
        var discount = Find(state, "Construcción eficiente"); Pick(state, discount);
        Check(data.Cost == 95, "Purchase price updates");
        Check(state.BlocksInput, "Closing click blocked");
        state.Choosing = false; UnityEngine.Time.frameCount++;
        Check(!state.BlocksInput, "Input resumes next frame");
        RunUpgradeState.Reset();
        Check(data.Damage == 10 && data.Cost == 100 && !RunUpgradeState.Current.Choosing,
            "New run resets modifiers and modal state");

        state = RunUpgradeState.Current;
        state.Choosing = true;
        var previous = state.Draw();
        int wallet = 100;
        bool Pay(int amount){ if (wallet < amount) return false; wallet -= amount; return true; }
        int firstCost = state.RerollCost;
        bool firstRerolled = state.TryReroll(previous, Pay, out var fresh);
        Check(firstCost == 30 && firstRerolled && wallet == 70,
            "Reroll spends exactly 30 gold");
        Check(fresh.Length == 3 && fresh.Distinct().Count() == 3 && !fresh.Intersect(previous).Any(),
            "Reroll offers three new distinct choices");
        int secondCost = state.RerollCost;
        bool secondRerolled = state.TryReroll(fresh, Pay, out var second);
        Check(state.Choosing && secondCost == 45 && secondRerolled && wallet == 25,
            "Second reroll costs 45 and keeps draft open");
        Check(!state.TryReroll(second, Pay, out _) && wallet == 25 && state.RerollCost == 60,
            "Insufficient gold does not charge or increase price");
        Pick(state, second[0]);
        Check(!state.TryReroll(second, Pay, out _) && wallet == 25, "No reroll outside draft");
        state.Choosing = true;
        Check(state.RerollCost == 60, "Price persists into next draft");
        var all = new HashSet<RunUpgradeState.Choice>();
        for (int i = 0; i < 500; i++) foreach (var choice in state.Draw()) all.Add(choice);
        var last = all.Take(4).ToArray();
        foreach (var choice in all) if (!last.Contains(choice)) choice.Stacks = choice.Limit;
        wallet = 1000;
        Check(state.TryReroll(last.Take(3).ToArray(), Pay, out var nearEnd) && nearEnd.Contains(last[3]),
            "Near exhaustion still guarantees a new option");
        foreach (var choice in all) choice.Stacks = choice.Limit;
        int unchangedWallet = wallet;
        Check(!state.TryReroll(nearEnd, Pay, out _) && wallet == unchangedWallet,
            "Exhausted catalog never charges gold");
        RunUpgradeState.Reset();
        Check(RunUpgradeState.Current.RerollCost == 30, "Reroll price resets with new run");
    }
}
public class TowerAttackData { }
public class CannonAttackData : TowerAttackData { }
public class LightningAttackData : TowerAttackData { }
public class FlameAttackData : TowerAttackData { }
public class ArcaneAttackData : TowerAttackData { }
namespace UnityEngine
{
    public class ScriptableObject { }
    public class SerializeField : Attribute { }
    public class CreateAssetMenuAttribute : Attribute { public string fileName; public string menuName; }
    public enum RuntimeInitializeLoadType { SubsystemRegistration }
    public class RuntimeInitializeOnLoadMethodAttribute : Attribute
    { public RuntimeInitializeOnLoadMethodAttribute(RuntimeInitializeLoadType type) { } }
    public static class Time { public static int frameCount; }
    public static class Mathf
    {
        public static int Min(int a, int b) => Math.Min(a,b);
        public static float Min(float a, float b) => Math.Min(a,b);
        public static int Max(int a, int b) => Math.Max(a,b);
        public static float Max(float a, float b) => Math.Max(a,b);
        public static float Clamp01(float value) => Math.Clamp(value, 0f, 1f);
        public static int CeilToInt(float value) => (int)Math.Ceiling(value);
    }
    public static class Random
    {
        static readonly System.Random rng = new System.Random(12345);
        public static int Range(int min, int max) => rng.Next(min,max);
        public static float value => (float)rng.NextDouble();
    }
}
