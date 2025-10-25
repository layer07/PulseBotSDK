using PulseBot.Models;

namespace PulseBot.Commands;

/// <summary>
/// Context provided to command handlers containing message, bot client, and helper methods.
/// </summary>
/// <param name="bot">Bot client instance</param>
/// <param name="message">Message that triggered the command</param>
/// <param name="args">Command arguments (excluding trigger)</param>
public sealed class CommandContext(PulseBot bot, BotMessage message, string[] args)
{
    /// <summary>
    /// Bot client instance for sending messages and reactions.
    /// </summary>
    public PulseBot Bot { get; } = bot;

    /// <summary>
    /// Original message that triggered this command.
    /// </summary>
    public BotMessage Message { get; } = message;

    /// <summary>
    /// Command arguments (words after trigger).
    /// </summary>
    public string[] Args { get; } = args;

    /// <summary>
    /// Check if command author is the bot owner.
    /// </summary>
    public bool IsOwner => bot.IsOwner(message.AuthorPublicKey);

    /// <summary>
    /// Get all arguments joined as a single string.
    /// </summary>
    public string ArgsText => string.Join(" ", args);

    /// <summary>
    /// Check if command has at least N arguments.
    /// </summary>
    public bool HasArgs(int count) => args.Length >= count;

    /// <summary>
    /// Reply to the triggering message.
    /// </summary>
    /// <param name="content">Reply content (Markdown supported)</param>
    public void Reply(string content) =>
        bot.ReplyToMessage(message.RoomID, message.MessageID, content);

    /// <summary>
    /// Send a new message to the same room (not a reply).
    /// </summary>
    /// <param name="content">Message content (Markdown supported)</param>
    public void Send(string content) =>
        bot.SendMessage(message.RoomID, content);

    /// <summary>
    /// React to the triggering message with a sticker.
    /// Includes automatic 100ms delay to prevent DOM race condition.
    /// </summary>
    /// <param name="stickerUuid">Sticker UUID to react with</param>
    public async Task React(Guid stickerUuid) =>
        await bot.ReactToMessage(message.MessageID, stickerUuid);

    /// <summary>
    /// Get a random sticker from available stickers.
    /// </summary>
    /// <returns>Random sticker or null if no stickers available</returns>
    public BotSticker? GetRandomSticker()
    {
        if (bot.Stickers.Count == 0) return null;
        return bot.Stickers[Random.Shared.Next(bot.Stickers.Count)];
    }

    /// <summary>
    /// Get argument at index or return default value.
    /// </summary>
    /// <param name="index">Argument index (0-based)</param>
    /// <param name="defaultValue">Default if index out of range</param>
    public string GetArg(int index, string defaultValue = "") =>
        index < args.Length ? args[index] : defaultValue;

    /// <summary>
    /// Try parse argument at index as integer.
    /// </summary>
    public bool TryGetInt(int index, out int value)
    {
        value = 0;
        return index < args.Length && int.TryParse(args[index], out value);
    }

    /// <summary>
    /// Try parse argument at index as GUID.
    /// </summary>
    public bool TryGetGuid(int index, out Guid value)
    {
        value = Guid.Empty;
        return index < args.Length && Guid.TryParse(args[index], out value);
    }

    /// <summary>
    /// Get arguments starting from index, joined as string.
    /// Useful for commands like: !say [message...]
    /// </summary>
    public string GetArgsFrom(int startIndex) =>
        startIndex < args.Length
            ? string.Join(" ", args[startIndex..])
            : "";
}