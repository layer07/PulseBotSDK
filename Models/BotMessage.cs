namespace PulseBot.Models;

/// <summary>
/// Represents a chat message received from the server.
/// </summary>
public sealed record BotMessage
{
    /// <summary>
    /// Unique message identifier.
    /// </summary>
    public required Guid MessageID { get; init; }

    /// <summary>
    /// Room where message was sent.
    /// </summary>
    public required Guid RoomID { get; init; }

    /// <summary>
    /// Public key of message author.
    /// </summary>
    public required Guid AuthorPublicKey { get; init; }

    /// <summary>
    /// Message content (Markdown format).
    /// </summary>
    public required string Content { get; init; }

    /// <summary>
    /// Author's username.
    /// </summary>
    public required string AuthorUsername { get; init; }

    /// <summary>
    /// Unix timestamp (milliseconds).
    /// </summary>
    public required long Timestamp { get; init; }

    /// <summary>
    /// ID of message being replied to (null if not a reply).
    /// </summary>
    public Guid? ReplyToID { get; init; }

    /// <summary>
    /// Convert Unix timestamp to DateTime.
    /// </summary>
    public DateTime GetDateTime() =>
        DateTimeOffset.FromUnixTimeMilliseconds(Timestamp).UtcDateTime;
}