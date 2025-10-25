namespace PulseBot.Components;

/// <summary>
/// Rich embed card (Discord-style).
/// </summary>
public sealed class Embed : BotComponent
{
    public override string Type => "embed";

    public string? Title { get; }
    public string? Description { get; }
    public string? Footer { get; }

    public Embed(string? title = null, string? description = null, string? footer = null)
    {
        Title = title;
        Description = description;
        Footer = footer;
    }

    public override string ToHtml()
    {
        return $@"
            <div class='bot-embed'>
                {(Title != null ? $"<div class='bot-embed-header'><h3 class='bot-embed-title'>{Esc(Title)}</h3></div>" : "")}
                {(Description != null ? $"<div class='bot-embed-description'>{Esc(Description)}</div>" : "")}
                {(Footer != null ? $"<div class='bot-embed-footer'>{Esc(Footer)}</div>" : "")}
            </div>
        ";
    }
}