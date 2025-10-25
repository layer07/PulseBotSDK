namespace PulseBot.Components;

/// <summary>
/// Video embed (local or YouTube).
/// </summary>
public sealed class VideoEmbed : BotComponent
{
    public override string Type => "video";

    public string Url { get; }

    public VideoEmbed(string url)
    {
        Url = url;
    }

    public override string ToHtml() =>
        $"<div class='bot-video-embed'><video controls width='100%' src='{Esc(Url)}'></video></div>";
}

/// <summary>
/// Image embed with optional styling.
/// </summary>
public sealed class ImageEmbed : BotComponent
{
    public override string Type => "image";

    public string Url { get; }
    public string? Alt { get; }

    public ImageEmbed(string url, string? alt = null)
    {
        Url = url;
        Alt = alt;
    }

    public override string ToHtml() =>
        $"<div class='bot-media-container'><img src='{Esc(Url)}' alt='{Esc(Alt ?? "Image")}' style='max-width: 100%; border-radius: 4px;'></div>";
}

/// <summary>
/// File download link.
/// </summary>
public sealed class FileDownload : BotComponent
{
    public override string Type => "file";

    public string Url { get; }
    public string Filename { get; }

    public FileDownload(string url, string filename)
    {
        Url = url;
        Filename = filename;
    }

    public override string ToHtml() =>
        $"<a href='{Esc(Url)}' download='{Esc(Filename)}' class='bot-file-download'><i class='fas fa-download'></i> {Esc(Filename)}</a>";
}