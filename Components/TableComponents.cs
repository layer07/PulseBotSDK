namespace PulseBot.Components;

/// <summary>
/// Data table with headers and rows.
/// </summary>
public sealed class Table : BotComponent
{
    public override string Type => "table";

    public string[] Headers { get; }
    public TableRow[] Rows { get; }

    public Table(string[] headers, params TableRow[] rows)
    {
        Headers = headers;
        Rows = rows;
    }

    public override string ToHtml()
    {
        var headerHtml = string.Join("", Headers.Select(h => $"<th>{Esc(h)}</th>"));
        var rowsHtml = string.Join("", Rows.Select(r => r.ToHtml()));

        return $@"
            <div class='bot-table-container'>
                <table class='bot-table'>
                    <thead><tr>{headerHtml}</tr></thead>
                    <tbody>{rowsHtml}</tbody>
                </table>
            </div>
        ";
    }
}

/// <summary>
/// Table row with cells.
/// </summary>
public sealed class TableRow : BotComponent
{
    public override string Type => "table-row";

    public TableCell[] Cells { get; }

    public TableRow(params TableCell[] cells)
    {
        Cells = cells;
    }

    public override string ToHtml() =>
        $"<tr>{string.Join("", Cells.Select(c => c.ToHtml()))}</tr>";
}

/// <summary>
/// Table cell with optional color coding.
/// </summary>
public sealed class TableCell : BotComponent
{
    public override string Type => "table-cell";

    public string Value { get; }
    public CellColor Color { get; }

    public TableCell(string value, CellColor color = CellColor.None)
    {
        Value = value;
        Color = color;
    }

    public override string ToHtml()
    {
        var colorClass = Color switch
        {
            CellColor.Positive => "positive",
            CellColor.Negative => "negative",
            CellColor.Neutral => "neutral",
            _ => ""
        };

        return $"<td class='{colorClass}'>{Esc(Value)}</td>";
    }
}

public enum CellColor
{
    None,
    Positive,
    Negative,
    Neutral
}