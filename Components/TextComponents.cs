namespace PulseBot.Components;

/// <summary>
/// Text block with optional strong emphasis.
/// </summary>
public sealed class TextBlock : BotComponent
{
    public override string Type => "text";

    public string Content { get; }
    public bool Strong { get; }

    public TextBlock(string content, bool strong = false)
    {
        Content = content;
        Strong = strong;
    }

    public override string ToHtml() =>
        Strong
            ? $"<p><strong>{Esc(Content)}</strong></p>"
            : $"<p>{Esc(Content)}</p>";
}

/// <summary>
/// Heading (h1-h4).
/// </summary>
public sealed class Heading : BotComponent
{
    public override string Type => "heading";

    public string Text { get; }
    public int Level { get; }

    public Heading(string text, int level = 3)
    {
        Text = text;
        Level = Math.Clamp(level, 1, 4);
    }

    public override string ToHtml() =>
        $"<h{Level}>{Esc(Text)}</h{Level}>";
}

/// <summary>
/// Key-value pair (label: value).
/// </summary>
public sealed class KeyValue : BotComponent
{
    public override string Type => "keyvalue";

    public string Key { get; }
    public string Value { get; }

    public KeyValue(string key, string value)
    {
        Key = key;
        Value = value;
    }

    public override string ToHtml() =>
        $"<p><strong>{Esc(Key)}:</strong> {Esc(Value)}</p>";
}