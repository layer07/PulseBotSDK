namespace PulseBot.Models;

/// <summary>
/// Represents a user in the chat system.
/// </summary>
public sealed record BotUser
{
    /// <summary>
    /// Unique public key identifier for this user.
    /// </summary>
    public required Guid PublicKey { get; init; }

    /// <summary>
    /// User's display name.
    /// </summary>
    public required string Username { get; init; }

    /// <summary>
    /// User's email address.
    /// </summary>
    public string Email { get; init; } = "";

    /// <summary>
    /// If this is a bot account, the public key of the bot's owner.
    /// Used for owner-only command authorization.
    /// </summary>
    public Guid? BotOwnerKey { get; init; }

    /// <summary>
    /// Check if this user is a bot.
    /// </summary>
    public bool IsBot => BotOwnerKey.HasValue;
}