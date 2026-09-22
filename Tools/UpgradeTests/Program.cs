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
        Check(seen.Count == 25, "25 choices, 500 offers without duplicate cards");
        var data = new TowerData { damage = 10, fireRate = 2, range = 6, cost = 100 };
        var damage = Find(state, "Arsenal reforzado");
        state.Apply(damage);
        Check(damage.Stacks == 0, "No selection outside draft");
        Pick(state, damage);
        state.Apply(damage);
        Check(damage.Stacks == 1 && Math.Abs(data.Damage - 10.6f) < .001f,
            "Exactly one choice per draft; effective damage");
        Check(data.damage == 10 && data.cost == 100, "Base asset values unchanged");
        Pick(state, damage); Pick(state, damage); Pick(state, damage);
        Check(damage.Stacks == 3, "Stack limit enforced");
        Check(Enumerable.Range(0, 500).All(_ => !state.Draw().Contains(damage)), "Capped upgrade excluded");
        var rate = Find(state, "Mecanismos ágiles"); Pick(state, rate);
        Check(Math.Abs(data.FireRate - 2.12f) < .001f, "Fire rate modifier");
        var area = Find(state, "Onda expansiva"); Pick(state, area);
        Check(Math.Abs(state.Multiplier("area", new CannonAttackData()) - 1.1f) < .001f &&
            state.Multiplier("area", new FlameAttackData()) == 1, "Family-specific area");
        var bounce = Find(state, "Arco adicional"); Pick(state, bounce); Pick(state, bounce); Pick(state, bounce);
        Check(state.Bonus("bounces", new LightningAttackData()) == 2, "Rebound limit");
        var discount = Find(state, "Construcción eficiente"); Pick(state, discount);
        Check(data.Cost == 95, "Purchase price updates");
        Check(state.BlocksInput, "Closing click blocked");
        state.Choosing = false; UnityEngine.Time.frameCount++;
        Check(!state.BlocksInput, "Input resumes next frame");
        RunUpgradeState.Reset();
        Check(data.Damage == 10 && data.Cost == 100 && !RunUpgradeState.Current.Choosing,
            "New run resets modifiers and modal state");
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
        public static int Max(int a, int b) => Math.Max(a,b);
        public static float Max(float a, float b) => Math.Max(a,b);
        public static int CeilToInt(float value) => (int)Math.Ceiling(value);
    }
    public static class Random
    {
        static readonly System.Random rng = new System.Random(12345);
        public static int Range(int min, int max) => rng.Next(min,max);
    }
}
