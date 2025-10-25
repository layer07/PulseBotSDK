using System.Xml.Linq;

namespace PulseBot;

/// <summary>
/// Immutable configuration record for PulseBot.
/// Supports loading from XML files with fallback defaults.
/// </summary>
/// <param name="ServerIP">Server IP address or hostname</param>
/// <param name="ServerPort">Server port number (1-65535)</param>
/// <param name="ApiKey">Bot API key for authentication</param>
/// <param name="Protocol">Transport protocol (TCP or UDP)</param>
/// <param name="BotName">Bot display name</param>
/// <param name="OwnerPublicKey">Manual owner public key (Guid.Empty for auto-detect)</param>
/// <param name="AutoDetectOwner">Enable automatic owner detection from server</param>
public sealed record PulseBotConfig(
    string ServerIP,
    int ServerPort,
    string ApiKey,
    string Protocol = "TCP",
    string BotName = "PulseBot",
    Guid OwnerPublicKey = default,
    bool AutoDetectOwner = true
)
{
    /// <summary>
    /// Default configuration with localhost settings.
    /// </summary>
    public static PulseBotConfig Default => new(
        ServerIP: "127.0.0.1",
        ServerPort: 1339,
        ApiKey: "",
        Protocol: "TCP"
    );

    /// <summary>
    /// Load configuration from XML file.
    /// Falls back to defaults if file not found.
    /// </summary>
    /// <param name="path">Path to app.xml configuration file</param>
    /// <returns>Loaded configuration or defaults</returns>
    public static PulseBotConfig LoadFromXml(string path = "app.xml")
    {
        try
        {
            var doc = XDocument.Load(path);
            var root = doc.Root ?? throw new InvalidOperationException("XML root element not found");

            Console.WriteLine($"[CONFIG] Loaded {path}");

            return new PulseBotConfig(
                ServerIP: GetValue(root, "Server/IP", "127.0.0.1"),
                ServerPort: GetInt(root, "Server/Port", 1339),
                ApiKey: GetValue(root, "Server/ApiKey", ""),
                Protocol: GetValue(root, "Server/Protocol", "TCP"),
                BotName: GetValue(root, "Bot/Name", "PulseBot"),
                OwnerPublicKey: GetGuid(root, "Bot/OwnerPublicKey", Guid.Empty),
                AutoDetectOwner: GetBool(root, "Bot/AutoDetectOwner", true)
            );
        }
        catch (FileNotFoundException)
        {
            Console.WriteLine($"[CONFIG] {path} not found - using defaults");
            return Default;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[CONFIG] Error loading {path}: {ex.Message} - using defaults");
            return Default;
        }
    }

    /// <summary>
    /// Validate configuration values.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when configuration is invalid</exception>
    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(ServerIP))
            throw new InvalidOperationException("Server IP cannot be empty");

        if (ServerPort is < 1 or > 65535)
            throw new InvalidOperationException($"Server port must be between 1-65535, got: {ServerPort}");

        if (string.IsNullOrWhiteSpace(ApiKey))
            throw new InvalidOperationException("API key cannot be empty. Set <ApiKey> in app.xml");

        if (Protocol is not ("TCP" or "UDP"))
            throw new InvalidOperationException($"Protocol must be TCP or UDP, got: {Protocol}");

        Console.WriteLine("[CONFIG] ✅ Validation passed");
    }

    /// <summary>
    /// Display configuration summary.
    /// </summary>
    public void PrintSummary()
    {
        Console.WriteLine("[CONFIG] Configuration:");
        Console.WriteLine($"  Server: {ServerIP}:{ServerPort}");
        Console.WriteLine($"  Protocol: {Protocol}");
        Console.WriteLine($"  Bot Name: {BotName}");
        Console.WriteLine($"  Auto-detect Owner: {AutoDetectOwner}");
    }

    // ========================================================================
    // XML Helper Methods (Private)
    // ========================================================================

    private static string GetValue(XElement root, string path, string defaultValue)
    {
        var parts = path.Split('/');
        XElement? current = root;

        foreach (var part in parts)
        {
            current = current?.Element(part);
            if (current is null) return defaultValue;
        }

        return current.Value ?? defaultValue;
    }

    private static int GetInt(XElement root, string path, int defaultValue)
    {
        var value = GetValue(root, path, "");
        return int.TryParse(value, out var result) ? result : defaultValue;
    }

    private static bool GetBool(XElement root, string path, bool defaultValue)
    {
        var value = GetValue(root, path, "");
        return bool.TryParse(value, out var result) ? result : defaultValue;
    }

    private static Guid GetGuid(XElement root, string path, Guid defaultValue)
    {
        var value = GetValue(root, path, "");
        return Guid.TryParse(value, out var result) ? result : defaultValue;
    }
}