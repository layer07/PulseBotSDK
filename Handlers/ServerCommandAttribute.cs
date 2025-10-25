namespace PulseBot.Handlers;

/// <summary>
/// Marks a method as a server command handler (auto-discovered via reflection).
/// Server commands are events sent FROM the server TO the bot.
/// Examples: CHAT_BOOTSTRAP, NEW_MESSAGE, GET_MY_SERVERS
/// </summary>
/// <example>
/// <code>
/// [ServerCommand("NEW_MESSAGE")]
/// private void HandleNewMessage(JsonElement root)
/// {
///     if (root.TryGetProperty("Message", out var msgProp))
///     {
///         var message = JsonSerializer.Deserialize&lt;BotMessage&gt;(msgProp.GetRawText());
///         // Process message...
///     }
/// }
/// </code>
/// </example>
/// <param name="will">Server command Will identifier (e.g., "CHAT_BOOTSTRAP")</param>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
public sealed class ServerCommandAttribute(string will) : Attribute
{
    /// <summary>
    /// Server command Will identifier.
    /// Must match the "Will" field in server JSON responses.
    /// </summary>
    public string Will { get; } = will;
}