using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;
using System.Net;

namespace PulseBot.Utilities;

/// <summary>
/// Minimal HTTP file server for serving downloaded files locally.
/// Runs on localhost:PORT and serves files from /downloads folder.
/// </summary>
public sealed class LocalFileServer
{
    private readonly int _port;
    private readonly string _rootPath;
    private WebApplication? _app;
    private Thread? _serverThread;

    public bool IsRunning { get; private set; }
    public string BaseUrl => $"http://localhost:{_port}";

    public LocalFileServer(int port = 8080, string? rootPath = null)
    {
        _port = port;
        _rootPath = rootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "downloads");

        // Ensure downloads folder exists
        Directory.CreateDirectory(_rootPath);
    }

    /// <summary>
    /// Start file server on background thread.
    /// </summary>
    public void Start()
    {
        if (IsRunning)
        {
            Console.WriteLine($"[FileServer] Already running on {BaseUrl}");
            return;
        }

        _serverThread = new Thread(() =>
        {
            try
            {
                var builder = WebApplication.CreateBuilder(Array.Empty<string>());

                // Disable logging
                builder.Logging.ClearProviders();

                // Configure Kestrel
                builder.WebHost.ConfigureKestrel(options =>
                {
                    options.Listen(IPAddress.Loopback, _port);
                });

                _app = builder.Build();

                // Serve static files from root path
                _app.UseStaticFiles(new StaticFileOptions
                {
                    FileProvider = new PhysicalFileProvider(_rootPath),
                    ServeUnknownFileTypes = true,
                    DefaultContentType = "application/octet-stream"
                });

                // Root endpoint (file listing)
                _app.MapGet("/", () =>
                {
                    var files = Directory.GetFiles(_rootPath)
                        .Select(f => Path.GetFileName(f))
                        .ToArray();

                    return Results.Json(new
                    {
                        server = "PulseBot Local File Server",
                        files = files,
                        count = files.Length
                    });
                });

                IsRunning = true;
                Console.WriteLine($"[FileServer] Started on {BaseUrl}");
                Console.WriteLine($"[FileServer] Serving from: {_rootPath}");

                _app.Run();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[FileServer] ERROR: {ex.Message}");
                IsRunning = false;
            }
        })
        {
            IsBackground = true,
            Name = "LocalFileServer"
        };

        _serverThread.Start();

        // Wait for server to start
        Thread.Sleep(500);
    }

    /// <summary>
    /// Stop file server.
    /// </summary>
    public async Task StopAsync()
    {
        if (!IsRunning || _app == null)
            return;

        await _app.StopAsync();
        IsRunning = false;
        Console.WriteLine("[FileServer] Stopped");
    }

    /// <summary>
    /// Get URL for a file in the downloads folder.
    /// </summary>
    public string GetFileUrl(string filename) =>
        $"{BaseUrl}/{Uri.EscapeDataString(filename)}";

    /// <summary>
    /// Save content to downloads folder and return URL.
    /// </summary>
    public string SaveFile(string filename, byte[] content)
    {
        var filePath = Path.Combine(_rootPath, filename);
        File.WriteAllBytes(filePath, content);
        Console.WriteLine($"[FileServer] Saved: {filename} ({content.Length} bytes)");
        return GetFileUrl(filename);
    }

    /// <summary>
    /// Save content to downloads folder and return URL.
    /// </summary>
    public async Task<string> SaveFileAsync(string filename, Stream content)
    {
        var filePath = Path.Combine(_rootPath, filename);
        await using var fileStream = File.Create(filePath);
        await content.CopyToAsync(fileStream);
        Console.WriteLine($"[FileServer] Saved: {filename}");
        return GetFileUrl(filename);
    }

    /// <summary>
    /// Delete file from downloads folder.
    /// </summary>
    public bool DeleteFile(string filename)
    {
        try
        {
            var filePath = Path.Combine(_rootPath, filename);
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
                Console.WriteLine($"[FileServer] Deleted: {filename}");
                return true;
            }
            return false;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[FileServer] Delete error: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// List all files in downloads folder.
    /// </summary>
    public string[] ListFiles() =>
        Directory.GetFiles(_rootPath)
            .Select(Path.GetFileName)
            .Where(name => name != null)
            .Cast<string>()
            .ToArray();

    /// <summary>
    /// Clear all files from downloads folder.
    /// </summary>
    public void ClearAll()
    {
        foreach (var file in Directory.GetFiles(_rootPath))
        {
            try { File.Delete(file); }
            catch { }
        }
        Console.WriteLine("[FileServer] Cleared all files");
    }
}