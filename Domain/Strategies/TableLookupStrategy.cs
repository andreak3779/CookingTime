// filepath: Domain/Strategies/TableLookupStrategy.cs
using CookingTime.Domain.Abstractions;
using CookingTime.Domain.ValueObjects;

namespace CookingTime.Domain.Strategies;

/// <summary>
/// Table-driven cooking-time lookup. Matches the input weight against
/// ordered rows of (minPounds, maxPounds, minTime, maxTime). Throws
/// <see cref="ArgumentOutOfRangeException"/> if no row matches.
/// </summary>
public sealed class TableLookupStrategy : ICookingStrategy
{
    private readonly IReadOnlyList<Row> _rows;

    public TableLookupStrategy(IEnumerable<(decimal MinPounds, decimal MaxPounds, TimeSpan Min, TimeSpan Max)> rows)
    {
        ArgumentNullException.ThrowIfNull(rows);
        _rows = rows
            .OrderBy(r => r.MinPounds)
            .Select(r => new Row(r.MinPounds, r.MaxPounds, r.Min, r.Max))
            .ToList();
        if (_rows.Count == 0)
            throw new ArgumentException("At least one row is required.", nameof(rows));
    }

    public CookingDuration Calculate(Weight weight)
    {
        var lbs = weight.Pounds;
        foreach (var row in _rows)
        {
            if (lbs >= row.MinPounds && lbs <= row.MaxPounds)
                return new CookingDuration(row.Min, row.Max);
        }
        throw new ArgumentOutOfRangeException(nameof(weight), weight.Pounds,
            $"Weight is outside the known cooking range ({_rows[0].MinPounds}-{_rows[^1].MaxPounds} lbs).");
    }

    private readonly record struct Row(decimal MinPounds, decimal MaxPounds, TimeSpan Min, TimeSpan Max);
}
