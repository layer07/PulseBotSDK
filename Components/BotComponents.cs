namespace PulseBot.Components;

/// <summary>
/// Base class for all bot surface components.
/// Components render to HTML on backend, sent to frontend for display.
/// </summary>
public abstract class BotComponent
{
    /// <summary>
    /// Component type identifier for frontend renderer.
    /// </summary>
    public abstract string Type { get; }

    /// <summary>
    /// Render component to HTML.
    /// </summary>
    public abstract string ToHtml();

    /// <summary>
    /// Escape HTML special characters.
    /// </summary>
    protected static string Esc(string text) =>
        text?.Replace("&", "&amp;")
            .Replace("<", "&lt;")
            .Replace(">", "&gt;")
            .Replace("\"", "&quot;")
            .Replace("'", "&#39;") ?? "";
}