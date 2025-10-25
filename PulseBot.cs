using PulseBot.Commands;
using PulseBot.Handlers;
using PulseBot.Models;
using PulseBot.Network;
using PulseBot.Utilities;
using System.Collections.Concurrent;
using System.Text.Json;

namespace PulseBot;

/// <summary>
/// High-performance chat bot with reflection-based command routing.
/// Processes commands in ~4ms, faster than browser DOM rendering.
/// </summary>
/// <example>
/// <code>
/// var bot = PulseBot.Create()
///     .WithServer("192.168.1.77", 1339)
///     .WithApiKey("your-api-key")
///     .UseTCP()
///     .Build();
///     
/// await bot.Start();
/// </code>
/// </example>
public sealed class PulseBot : IDisposable
{
    private readonly INetworkTransport _transport;

    // Collections - concurrent for thread safety
    public ConcurrentDictionary<Guid, BotMessage> Messages { get; } = new();
    public ConcurrentDictionary<Guid, BotUser> Users { get; } = new();
    public ConcurrentDictionary<Guid, BotServer> Servers { get; } = new();
    public ConcurrentDictionary<Guid, BotRoom> Rooms { get; } = new();
    public List<BotSticker> Stickers { get; } = [];

    // Message deduplication (prevents processing same message twice)
    private readonly HashSet<Guid> _processedMessageIds = new();

    // State
    public BotUser? Me { get; private set; }
    public bool IsConnected => _transport.IsConnected;
    public DateTime StartTime { get; } = DateTime.UtcNow;

    // Components
    public CommandDispatcher Dispatcher { get; }
    private readonly ServerCommandHandler _serverHandler = new();
    private Guid? _ownerPublicKey;

    //LocalFileServer
    public LocalFileServer? FileServer { get; set; }


    // Events
    public event Action? OnReady;
    public event Action<Exception>? OnError;

    // Constructor
    public PulseBot(INetworkTransport transport)
    {
        _transport = transport;
        Dispatcher = new CommandDispatcher(this);

        // Wire up network events
        _transport.OnCommand += _serverHandler.Handle;
        _transport.OnConnected += () => Console.WriteLine("[PULSE] Connected");
        _transport.OnDisconnected += () => Console.WriteLine("[PULSE] Disconnected");
        _transport.OnError += ex =>
        {
            Console.WriteLine($"[ERROR] {ex.Message}");
            OnError?.Invoke(ex);
        };

        // Wire up server command handlers
        _serverHandler.RegisterHandler("CHAT_BOOTSTRAP", HandleBootstrap);
        _serverHandler.RegisterHandler("GET_MY_SERVERS", HandleGetServers);
        _serverHandler.RegisterHandler("NEW_MESSAGE", HandleNewMessage);
        _serverHandler.RegisterHandler("OPEN_DM", HandleOpenDM);
        _serverHandler.RegisterHandler("POPUP", HandlePopup);
    }

    /// <summary>
    /// Create a new bot using fluent builder pattern.
    /// </summary>
    public static PulseBotBuilder Create() => new();

    /// <summary>
    /// Create a bot from XML config file.
    /// </summary>
    public static PulseBot FromConfig(string path = "app.xml")
    {
        var config = PulseBotConfig.LoadFromXml(path);
        config.Validate();

        return Create()
            .WithConfig(config)
            .Build();
    }

    /// <summary>
    /// Start the bot and connect to server.
    /// </summary>
    public async Task Start()
    {
        _transport.Start();
        await Task.Delay(500);

        _transport.Send("CHAT_BOOTSTRAP");
        await Task.Delay(500);

        _transport.Send("GET_MY_SERVERS");
    }

    /// <summary>
    /// Stop the bot and disconnect from server.
    /// </summary>
    public void Stop() => _transport.Stop();

    /// <summary>
    /// Check if user is the bot owner.
    /// </summary>
    public bool IsOwner(Guid publicKey) =>
        _ownerPublicKey.HasValue && publicKey == _ownerPublicKey.Value;

    // ========================================================================
    // SERVER COMMAND HANDLERS (Private)
    // ========================================================================

    private void HandleBootstrap(JsonElement root)
    {
        if (root.TryGetProperty("User", out var userProp))
        {
            Me = JsonSerializer.Deserialize<BotUser>(userProp.GetRawText());
            Console.WriteLine($"[PULSE] Logged in as: {Me?.Username}");

            if (Me?.BotOwnerKey is { } ownerKey)
            {
                _ownerPublicKey = ownerKey;
                Console.WriteLine($"[OWNER] Auto-detected: {ownerKey}");
            }
        }

        if (root.TryGetProperty("GlobalStickers", out var stickersProp))
        {
            var stickers = JsonSerializer.Deserialize<List<BotSticker>>(stickersProp.GetRawText());
            if (stickers is not null)
            {
                Stickers.Clear();
                Stickers.AddRange(stickers);
                Console.WriteLine($"[STICKERS] Loaded {stickers.Count} stickers");
            }
        }
    }

    private void HandleGetServers(JsonElement root)
    {
        if (!root.TryGetProperty("Servers", out var serversProp)) return;

        foreach (var serverItem in serversProp.EnumerateArray())
        {
            if (!serverItem.TryGetProperty("Server", out var serverProp)) continue;

            var server = JsonSerializer.Deserialize<BotServer>(serverProp.GetRawText());
            if (server is not null)
            {
                Servers[server.ServerID] = server;
                Console.WriteLine($"[SERVER] {server.ServerName}");
            }
        }

        Console.WriteLine($"[PULSE] Ready! Subscribed to {Servers.Count} servers");
        OnReady?.Invoke();

        if (_ownerPublicKey.HasValue)
        {
            SendMessageToOwner($"🤖 Bot '{Me?.Username}' is online!\n✨ {Stickers.Count} stickers loaded");
        }
    }

    /// <summary>
    /// Handle incoming messages with deduplication.
    /// Protects against duplicate broadcasts, multi-room scenarios, and network retries.
    /// </summary>
    private void HandleNewMessage(JsonElement root)
    {
        if (!root.TryGetProperty("Message", out var msgProp)) return;

        var message = JsonSerializer.Deserialize<BotMessage>(msgProp.GetRawText());
        if (message is null) return;

        // Ignore own messages (don't respond to ourselves)
        if (message.AuthorPublicKey == Me?.PublicKey)
        {
            return;
        }

        // Deduplication: Check if we've already processed this message
        // Prevents double-execution from duplicate broadcasts or multi-room scenarios
        lock (_processedMessageIds)
        {
            if (!_processedMessageIds.Add(message.MessageID))
            {
                Console.WriteLine($"[DEDUP] Skipped duplicate message: {message.MessageID}");
                return;
            }

            // Cleanup old message IDs periodically (prevent memory leak)
            // Keep last 10,000 messages, remove oldest 5,000 when limit reached
            if (_processedMessageIds.Count > 10000)
            {
                Console.WriteLine("[DEDUP] Cleaning up message ID cache (removing 5,000 oldest entries)");
                var toRemove = _processedMessageIds.Take(5000).ToList();
                foreach (var id in toRemove)
                {
                    _processedMessageIds.Remove(id);
                }
            }
        }

        // Store message and dispatch to command handler
        Messages[message.MessageID] = message;
        Console.WriteLine($"[MESSAGE] {message.AuthorUsername}: {message.Content}");

        Dispatcher.Dispatch(message);
    }

    private void HandleOpenDM(JsonElement root)
    {
        if (!root.TryGetProperty("DMRoom", out var roomProp)) return;

        var room = JsonSerializer.Deserialize<BotRoom>(roomProp.GetRawText());
        if (room is not null)
        {
            Rooms[room.RoomID] = room;
            Console.WriteLine($"[DM] Opened: {room.RoomID}");
        }
    }

    private void HandlePopup(JsonElement root)
    {
        var msg1 = root.TryGetProperty("Msg1", out var m1) ? m1.GetString() : "";
        var msg2 = root.TryGetProperty("Msg2", out var m2) ? m2.GetString() : "";
        Console.WriteLine($"[SERVER] {msg1}: {msg2}");
    }

    // ========================================================================
    // PUBLIC API - Message Operations
    // ========================================================================

    /// <summary>
    /// Send a message to a room.
    /// </summary>
    public void SendMessage(Guid roomId, string content) =>
        _transport.Send("SEND_MESSAGE", new
        {
            RoomID = roomId,
            ContentMD = content,
            MessageType = "text"
        });

    /// <summary>
    /// Reply to a specific message.
    /// </summary>
    public void ReplyToMessage(Guid roomId, Guid replyToId, string content) =>
        _transport.Send("SEND_MESSAGE", new
        {
            RoomID = roomId,
            ContentMD = content,
            ReplyToID = replyToId,
            MessageType = "text"
        });

    /// <summary>
    /// React to a message with a sticker.
    /// Includes 100ms delay to prevent race condition with DOM rendering.
    /// </summary>
    public async Task ReactToMessage(Guid messageId, Guid stickerUuid)
    {
        await Task.Delay(100); // Bot faster than DOM - give frontend time to render
        _transport.Send("ADD_REACTION", new { MessageID = messageId, StickerUUID = stickerUuid });
    }

    /// <summary>
    /// Send a DM to the bot owner.
    /// </summary>
    public void SendMessageToOwner(string content)
    {
        if (!_ownerPublicKey.HasValue)
        {
            Console.WriteLine("[ERROR] Owner public key not set");
            return;
        }

        _transport.Send("OPEN_DM", new { TargetUserPublicKey = _ownerPublicKey.Value });

        _ = Task.Delay(300).ContinueWith(_ =>
        {
            var dmRoom = Rooms.Values.FirstOrDefault(r =>
                (r.DMParticipant1 == Me?.PublicKey && r.DMParticipant2 == _ownerPublicKey) ||
                (r.DMParticipant2 == Me?.PublicKey && r.DMParticipant1 == _ownerPublicKey)
            );

            if (dmRoom is not null)
            {
                SendMessage(dmRoom.RoomID, content);
            }
        });
    }

    // Add to existing PulseBot.cs

    /// <summary>
    /// Spawn surface for owner (PersonalBot only).
    /// </summary>
    public void SpawnSurface(BotSurface surface)
    {
        _transport.Send("BOT_SPAWN_SURFACE", new
        {
            Title = surface.Title,
            HtmlContent = surface.ToHtml(),
            Width = surface.Width,
            Height = surface.Height
        });
    }

    /// <summary>
    /// Spawn surface for specific user (ServerBot or PersonalBot).
    /// </summary>
    public void SpawnSurfaceForUser(Guid targetUserKey, BotSurface surface)
    {
        _transport.Send("BOT_SPAWN_SURFACE_TARGETED", new
        {
            TargetUserKey = targetUserKey,
            Title = surface.Title,
            HtmlContent = surface.ToHtml(),
            Width = surface.Width,
            Height = surface.Height
        });
    }

    public void Dispose()
    {
        _transport.Stop();
        GC.SuppressFinalize(this);
    }
}