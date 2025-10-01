using System;
using System.Collections.Generic;

/// <summary>
/// Розрахунок ціни апгрейду статів на клієнті.
/// Правила:
/// - базовий рівень усіх статів = 5
/// - перша прокачка 5→6 коштує завжди 5 зелені
/// - далі: price = round_away( (p*x^2 + q*x + r) * scale ), де x = level - 5
/// - порядок дорожнечі: Power > Skill > Survivability > Protection > Dexterity
/// </summary>
public static class StatCostClient
{
    public enum Stat
    {
        Power,
        Skill,
        Survivability,
        Protection,
        Dexterity
    }

    // Коефіцієнти полінома (p, q, r)
    private static readonly Dictionary<Stat, (double p, double q, double r)> K = new()
    {
        { Stat.Power,          (3.8, -6.4, 3.6) },
        { Stat.Skill,          (3.0, -5.0, 3.0) },
        { Stat.Survivability,  (2.6, -3.8, 2.2) },
        { Stat.Protection,     (2.8, -4.4, 2.6) }, // між Skill і Survivability
        { Stat.Dexterity,      (1.8, -1.4, 0.6) },
    };

    // Пер-статові масштаби, відкалібровані під твої якорі 20→21:
    // Power=1351, Skill=1024, Survivability=891, Protection=676, Dexterity=588
    private static readonly Dictionary<Stat, double> Scale = new()
    {
        { Stat.Power,         1.771 }, // ≈ під 1351
        { Stat.Skill,         1.698 }, // ≈ під 1024
        { Stat.Survivability, 1.680 }, // ≈ під 891
        { Stat.Protection,    1.193 }, // ≈ під 676
        { Stat.Dexterity,     1.529 }, // ≈ під 588
    };

    /// <summary>
    /// Повертає ціну апгрейду для переходу з level на level+1.
    /// level: поточний рівень (мінімум 5).
    /// </summary>
    public static int GetPrice(Stat stat, int level)
    {
        if (level <= 5) return 5; // 5→6 = 5

        int price = PolyRounded(stat, level);
        price = Math.Max(5, price); // мінімум 5

        // монотонна гарантія: не менше попереднього кроку
        int prev = (level - 1 <= 5) ? 5 : Math.Max(5, PolyRounded(stat, level - 1));
        if (price < prev) price = prev;

        return price;
    }

    private static int PolyRounded(Stat stat, int level)
    {
        double x = level - 5.0;
        var (p, q, r) = K[stat];
        double s = Scale[stat];
        double v = (p * x * x + q * x + r) * s;
        return RoundAwayFromZero(v);
    }

    public static string ToWireKey(Stat stat) => stat switch
    {
        Stat.Power         => "power",
        Stat.Skill         => "skill",
        Stat.Survivability => "survivability",
        Stat.Protection    => "protection",
        Stat.Dexterity     => "dexterity",
        _ => "power"
    };

    public static Stat FromWireKey(string key) => key?.ToLowerInvariant() switch
    {
        "power"          => Stat.Power,
        "skill"          => Stat.Skill,
        "survivability"  => Stat.Survivability,
        "protection"     => Stat.Protection,
        "dexterity"      => Stat.Dexterity,
        _                => Stat.Power
    };

    private static int RoundAwayFromZero(double v)
        => (int)Math.Round(v, MidpointRounding.AwayFromZero);
}
