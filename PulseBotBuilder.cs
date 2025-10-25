using System.Xml.Linq;
using PulseBot.Network;

namespace PulseBot;

/// <summary>
/// Fluent builder for constructing PulseBot instances.
/// Provides chainable API for configuration.
/// </summary>
/// <example>
/// <code>
/// var bot = PulseBot.Create()
///     .WithServer("192.168.1.77", 1339)
///     .WithApiKey("your-api-key")
///     .UseTCP()
///     .Build();
/// </code>
/// </example>
public sealed class PulseBotBuilder
{
    private string? _serverIp;
    private int _serverPort = 1339;
    private string? _apiKey;
    private TransportProtocol _protocol = TransportProtocol.TCP;
    private string _botName = "PulseBot";
    private Guid _ownerPublicKey = Guid.Empty;
    private bool _autoDetectOwner = true;

    /// <summary>
    /// Configure server connection details.
    /// </summary>
    public PulseBotBuilder WithServer(string ip, int port)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(ip);
        ArgumentOutOfRangeException.ThrowIfLessThan(port, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(port, 65535);

        _serverIp = ip;
        _serverPort = port;
        return this;
    }

    /// <summary>
    /// Set bot API key for authentication.
    /// </summary>
    public PulseBotBuilder WithApiKey(string apiKey)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(apiKey);
        _apiKey = apiKey;
        return this;
    }

    /// <summary>
    /// Use TCP transport (recommended for production).
    /// </summary>
    public PulseBotBuilder UseTCP()
    {
        _protocol = TransportProtocol.TCP;
        return this;
    }

    /// <summary>
    /// Use UDP transport (legacy, best-effort delivery).
    /// </summary>
    public PulseBotBuilder UseUDP()
    {
        _protocol = TransportProtocol.UDP;
        return this;
    }

    /// <summary>
    /// Set bot display name.
    /// </summary>
    public PulseBotBuilder WithName(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        _botName = name;
        return this;
    }

    /// <summary>
    /// Manually set owner public key (overrides auto-detection).
    /// </summary>
    public PulseBotBuilder WithOwner(Guid ownerPublicKey)
    {
        _ownerPublicKey = ownerPublicKey;
        _autoDetectOwner = false;
        return this;
    }

    /// <summary>
    /// Enable automatic owner detection from server bootstrap.
    /// </summary>
    public PulseBotBuilder WithOwnerAutoDetect()
    {
        _autoDetectOwner = true;
        return this;
    }

    /// <summary>
    /// Load configuration from PulseBotConfig instance.
    /// </summary>
    public PulseBotBuilder WithConfig(PulseBotConfig config)
    {
        ArgumentNullException.ThrowIfNull(config);

        _serverIp = config.ServerIP;
        _serverPort = config.ServerPort;
        _apiKey = config.ApiKey;
        _protocol = Enum.Parse<TransportProtocol>(config.Protocol, ignoreCase: true);
        _botName = config.BotName;
        _ownerPublicKey = config.OwnerPublicKey;
        _autoDetectOwner = config.AutoDetectOwner;

        return this;
    }

    /// <summary>
    /// Load configuration from XML file.
    /// </summary>
    public PulseBotBuilder WithConfigFile(string path = "app.xml")
    {
        var config = PulseBotConfig.LoadFromXml(path);
        return WithConfig(config);
    }

    /// <summary>
    /// Build the PulseBot instance.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when required configuration is missing.</exception>
    public PulseBot Build()
    {
        // Validation
        if (string.IsNullOrWhiteSpace(_serverIp))
            throw new InvalidOperationException("Server IP must be configured. Use WithServer() or WithConfigFile().");

        if (string.IsNullOrWhiteSpace(_apiKey))
            throw new InvalidOperationException("API key must be configured. Use WithApiKey() or WithConfigFile().");

        // Create transport
        INetworkTransport transport = _protocol switch
        {
            TransportProtocol.TCP => new TcpTransport(_serverIp, _serverPort, _apiKey),
            TransportProtocol.UDP => new UdpTransport(_serverIp, _serverPort, _apiKey),
            _ => throw new ArgumentOutOfRangeException(nameof(_protocol), $"Unknown protocol: {_protocol}")
        };

        // Create bot instance
        var bot = new PulseBot(transport);

        Console.WriteLine($"[BUILD] PulseBot configured:");
        Console.WriteLine($"  Server: {_serverIp}:{_serverPort}");
        Console.WriteLine($"  Protocol: {_protocol}");
        Console.WriteLine($"  Name: {_botName}");
        Console.WriteLine($"  Auto-detect owner: {_autoDetectOwner}");

        return bot;
    }
}

/// <summary>
/// Network transport protocol options.
/// </summary>
public enum TransportProtocol
{
    /// <summary>TCP - Guaranteed delivery, ordered messages (recommended)</summary>
    TCP,

    /// <summary>UDP - Best-effort delivery, no ordering guarantees (legacy)</summary>
    UDP
}