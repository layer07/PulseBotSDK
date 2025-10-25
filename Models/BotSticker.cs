namespace PulseBot.Models;

/// <summary>
/// Represents a sticker/emoji used for message reactions.
/// </summary>
public sealed record BotSticker
{
    /// <summary>
    /// Unique sticker identifier.
    /// </summary>
    public required Guid StickerUUID { get; init; }

    /// <summary>
    /// Sticker display name.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Sticker scope (Global, Server, User).
    /// </summary>
    public string Scope { get; init; } = "Global";

    /// <summary>
    /// Owner ID if scope is Server or User.
    /// </summary>
    public Guid? ScopeOwnerID { get; init; }

    /// <summary>
    /// Sticker image path (relative URL).
    /// </summary>
    public string? ImagePath { get; init; }

    /// <summary>
    /// Check if sticker is globally available.
    /// </summary>
    public bool IsGlobal => Scope == "Global";

    /// <summary>
    /// Check if sticker is server-specific.
    /// </summary>
    public bool IsServerSticker => Scope == "Server" && ScopeOwnerID.HasValue;

    /// <summary>
    /// Format sticker for display.
    /// </summary>
    public override string ToString() => $":{Name}: ({Scope})";
}