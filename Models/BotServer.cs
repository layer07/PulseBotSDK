namespace PulseBot.Models;

/// <summary>
/// Represents a chat server (Discord "guild" equivalent).
/// </summary>
public sealed record BotServer
{
    /// <summary>
    /// Unique server identifier.
    /// </summary>
    public required Guid ServerID { get; init; }

    /// <summary>
    /// Server display name.
    /// </summary>
    public required string ServerName { get; init; }

    /// <summary>
    /// Server description text.
    /// </summary>
    public string Description { get; init; } = "";

    /// <summary>
    /// Number of rooms/channels in this server.
    /// </summary>
    public int RoomCount { get; init; }

    /// <summary>
    /// Server icon path (relative URL).
    /// </summary>
    public string? IconPath { get; init; }

    /// <summary>
    /// Whether server is publicly discoverable.
    /// </summary>
    public bool IsPublic { get; init; }
}