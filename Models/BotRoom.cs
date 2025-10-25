namespace PulseBot.Models;

/// <summary>
/// Represents a chat room/channel.
/// Can be a server text channel or a direct message (DM).
/// </summary>
public sealed record BotRoom
{
    /// <summary>
    /// Unique room identifier.
    /// </summary>
    public required Guid RoomID { get; init; }

    /// <summary>
    /// Room display name.
    /// </summary>
    public required string RoomName { get; init; }

    /// <summary>
    /// Parent server ID (use special @me server GUID for DMs).
    /// </summary>
    public Guid ParentServerID { get; init; }

    /// <summary>
    /// Room topic/description.
    /// </summary>
    public string Topic { get; init; } = "";

    /// <summary>
    /// Room type (TextChannel, VoiceChannel, DirectMessage, etc).
    /// </summary>
    public string RoomType { get; init; } = "TextChannel";

    /// <summary>
    /// First participant in DM (null for server channels).
    /// </summary>
    public Guid? DMParticipant1 { get; init; }

    /// <summary>
    /// Second participant in DM (null for server channels).
    /// </summary>
    public Guid? DMParticipant2 { get; init; }

    /// <summary>
    /// Check if this room is a direct message.
    /// </summary>
    public bool IsDirectMessage =>
        RoomType == "DirectMessage" ||
        (DMParticipant1.HasValue && DMParticipant2.HasValue);

    /// <summary>
    /// Get DM partner's public key (if this is a DM and we know our own key).
    /// </summary>
    public Guid? GetDMPartner(Guid myPublicKey)
    {
        if (!IsDirectMessage) return null;

        if (DMParticipant1 == myPublicKey) return DMParticipant2;
        if (DMParticipant2 == myPublicKey) return DMParticipant1;

        return null;
    }
}