using System.Text.Json;

namespace PulseBot.Handlers;

/// <summary>
/// Routes server commands to registered handlers.
/// Server commands are events sent FROM the server TO the bot (e.g., NEW_MESSAGE, CHAT_BOOTSTRAP).
/// </summary>
public sealed class ServerCommandHandler
{
    private readonly Dictionary<string, Action<JsonElement>> _handlers = new();

    private static readonly HashSet<string> _ignoredUnhandled = new(StringComparer.OrdinalIgnoreCase)
{
    "SEND_MESSAGE",
    "BOT_SPAWN_SURFACE",
    "BOT_SPAWN_SURFACE_TARGETED",
    "BOT_SURFACE_ACTION",
    "PING"
};

    /// <summary>
    /// Register a handler for a specific server command.
    /// </summary>
    /// <param name="will">Server command Will identifier</param>
    /// <param name="handler">Handler that processes the command's JSON payload</param>
    public void RegisterHandler(string will, Action<JsonElement> handler)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(will);
        ArgumentNullException.ThrowIfNull(handler);

        _handlers[will.ToUpperInvariant()] = handler;
        Console.WriteLine($"[SERVER HANDLER] Registered: {will}");
    }

    /// <summary>
    /// Handle incoming server command.
    /// Routes to appropriate handler based on Will identifier.
    /// </summary>
    /// <param name="will">Command Will identifier</param>
    /// <param name="payload">Command payload (JsonElement from network transport)</param>
    public void Handle(string will, object? payload)
    {
        if (payload is null)
        {
            Console.WriteLine($"[SERVER HANDLER] Null payload for: {will}");
            return;
        }

        try
        {
            var normalizedWill = will.ToUpperInvariant();

            if (!_handlers.TryGetValue(normalizedWill, out var handler))
            {
                if (!_ignoredUnhandled.Contains(will))
                    Console.WriteLine($"[UNHANDLED] {will}");
                return;
            }

            // Convert payload to JsonElement for handler
            JsonElement root;

            if (payload is JsonElement element)
            {
                // Already a JsonElement from transport
                root = element;
            }
            else
            {
                // Fallback: serialize then deserialize (shouldn't happen with modern transports)
                var json = JsonSerializer.Serialize(payload);
                using var doc = JsonDocument.Parse(json);
                root = doc.RootElement.Clone(); // Clone to outlive the 'using' scope
            }

            handler(root);
        }
        catch (JsonException ex)
        {
            Console.WriteLine($"[HANDLER ERROR] {will}: JSON parsing failed - {ex.Message}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[HANDLER ERROR] {will}: {ex.Message}");
            Console.WriteLine(ex.StackTrace);
        }
    }

    /// <summary>
    /// Check if a handler is registered for a command.
    /// </summary>
    public bool HasHandler(string will) =>
        _handlers.ContainsKey(will.ToUpperInvariant());

    /// <summary>
    /// Get all registered command Will identifiers.
    /// </summary>
    public IEnumerable<string> GetRegisteredCommands() =>
        _handlers.Keys.OrderBy(k => k);

    /// <summary>
    /// Unregister a handler.
    /// </summary>
    public bool UnregisterHandler(string will) =>
        _handlers.Remove(will.ToUpperInvariant());

    /// <summary>
    /// Clear all registered handlers.
    /// </summary>
    public void ClearHandlers()
    {
        _handlers.Clear();
        Console.WriteLine("[SERVER HANDLER] All handlers cleared");
    }

    /// <summary>
    /// Get count of registered handlers.
    /// </summary>
    public int HandlerCount => _handlers.Count;
}