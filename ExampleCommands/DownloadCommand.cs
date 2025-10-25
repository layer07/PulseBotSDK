using PulseBot.Commands;
using PulseBot.Components;
using PulseBot.Models;
using System.Net.Http;

namespace PulseBot.ExampleCommands;

public static class DownloadCommand
{
    private static readonly HttpClient _httpClient = new();

    [Command("!dl", Description = "Download file from URL", MinArgs = 1)]
    public static async void Download(CommandContext ctx)
    {
        var url = ctx.GetArg(0);

        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
        {
            ctx.Reply("❌ Invalid URL");
            return;
        }

        ctx.Reply("⬇️ Downloading...");

        try
        {
            // Download file
            var response = await _httpClient.GetAsync(uri);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsByteArrayAsync();
            var contentType = response.Content.Headers.ContentType?.MediaType ?? "application/octet-stream";

            // Generate filename
            var filename = Path.GetFileName(uri.LocalPath);
            if (string.IsNullOrEmpty(filename) || filename == "/")
            {
                var ext = GetExtensionFromMimeType(contentType);
                filename = $"download_{DateTime.UtcNow:yyyyMMdd_HHmmss}{ext}";
            }

            // Save to file server
            var fileUrl = ctx.Bot.FileServer!.SaveFile(filename, content);

            // Format file size
            var sizeFormatted = FormatBytes(content.Length);

            // Determine file type
            var isImage = contentType.StartsWith("image/");
            var isVideo = contentType.StartsWith("video/");

            // Build surface based on file type
            var components = new List<BotComponent>
            {
                new Heading("✅ Download Complete"),
                new KeyValue("URL", uri.Host + uri.PathAndQuery),
                new KeyValue("File", filename),
                new KeyValue("Size", sizeFormatted),
                new KeyValue("Type", contentType),
                new Divider()
            };

            // Add preview if image
            if (isImage)
            {
                components.Add(new Heading("Preview", 4));
                components.Add(new ImageEmbed(fileUrl, filename));
                components.Add(new Divider());
            }

            // Add video player if video
            if (isVideo)
            {
                components.Add(new Heading("Preview", 4));
                components.Add(new VideoEmbed(fileUrl));
                components.Add(new Divider());
            }

            // Download button
            components.Add(new FileDownload(fileUrl, filename));
            components.Add(new ButtonRow(new Button("close", "Close")));

            ctx.Bot.SpawnSurface(new BotSurface
            {
                Title = "File Downloaded",
                Components = components.ToArray()
            });
        }
        catch (HttpRequestException ex)
        {
            ctx.Reply($"❌ Download failed: {ex.Message}");
        }
        catch (Exception ex)
        {
            ctx.Reply($"❌ Error: {ex.Message}");
        }
    }

    private static string GetExtensionFromMimeType(string mimeType) => mimeType switch
    {
        "image/jpeg" => ".jpg",
        "image/png" => ".png",
        "image/gif" => ".gif",
        "image/webp" => ".webp",
        "video/mp4" => ".mp4",
        "video/webm" => ".webm",
        "application/pdf" => ".pdf",
        "text/plain" => ".txt",
        _ => ".bin"
    };

    private static string FormatBytes(long bytes)
    {
        string[] sizes = { "B", "KB", "MB", "GB" };
        double len = bytes;
        int order = 0;
        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len /= 1024;
        }
        return $"{len:0.##} {sizes[order]}";
    }
}