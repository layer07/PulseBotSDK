namespace PulseBot.Models;

using global::PulseBot.Components;


/// <summary>
/// Bot surface definition with components.
/// </summary>
public sealed class BotSurface
{
    public string Title { get; set; } = "Bot Surface";
    public BotComponent[] Components { get; set; } = Array.Empty<BotComponent>();
    public int Width { get; set; } = 600;
    public int Height { get; set; } = 400;

    /// <summary>
    /// Render all components to HTML.
    /// </summary>
    public string ToHtml() =>
        string.Join("\n", Components.Select(c => c.ToHtml()));
}