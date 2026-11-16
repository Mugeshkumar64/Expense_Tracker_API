namespace Expense_Tracker.Common;

public static class UnitConverter
{
    private static readonly Dictionary<string, (string BaseUnit, decimal Factor)> UnitMap =
        new(StringComparer.OrdinalIgnoreCase)
        {
            { "piece", ("piece", 1m) },
            { "kg", ("kg", 1m) },
            { "g", ("kg", 1000m) },
            { "litre", ("litre", 1m) },
            { "ml", ("litre", 1000m) },
            { "packet", ("packet", 1m) },
            { "dozen", ("dozen", 1m) }
        };

    public static (string BaseUnit, decimal Factor) GetUnitInfo(string unit)
    {
        if (string.IsNullOrWhiteSpace(unit))
            return ("kg", 1m);

        if (UnitMap.TryGetValue(unit.Trim(), out var info))
            return info;

        return (unit.Trim().ToLowerInvariant(), 1m);
    }

    public static decimal NormalizePrice(decimal unitPrice, string unit)
    {
        var (_, factor) = GetUnitInfo(unit);
        return unitPrice * factor;
    }

    public static decimal NormalizeQuantity(decimal quantity, string unit)
    {
        var (_, factor) = GetUnitInfo(unit);
        return factor > 0 ? quantity / factor : quantity;
    }
}
