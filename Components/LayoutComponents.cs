namespace PulseBot.Components;

/// <summary>
/// Horizontal divider line.
/// </summary>
public sealed class Divider : BotComponent
{
    public override string Type => "divider";

    public override string ToHtml() =>
        "<hr class='bot-divider'>";
}

/// <summary>
/// Status indicator badge.
/// </summary>
public sealed class Status : BotComponent
{
    public override string Type => "status";

    public string Text { get; }
    public StatusType StatusType { get; }

    public Status(string text, StatusType statusType)
    {
        Text = text;
        StatusType = statusType;
    }

    public override string ToHtml()
    {
        var typeClass = StatusType.ToString().ToLower();
        return $"<span class='bot-status {typeClass}'>{Esc(Text)}</span>";
    }
}

public enum StatusType
{
    Success,
    Error,
    Warning,
    Info
}

/// <summary>
/// Progress bar (0-100%).
/// </summary>
public sealed class ProgressBar : BotComponent
{
    public override string Type => "progress";

    public int Percentage { get; }

    public ProgressBar(int percentage)
    {
        Percentage = Math.Clamp(percentage, 0, 100);
    }

    public override string ToHtml() =>
        $@"
            <div class='bot-progress-container'>
                <div class='bot-progress-bar' style='width: {Percentage}%'>{Percentage}%</div>
            </div>
        ";
}