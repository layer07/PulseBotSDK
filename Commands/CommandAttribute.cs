namespace PulseBot.Commands;

/// <summary>
/// Marks a static method as a bot command handler.
/// Commands are auto-discovered via reflection at startup.
/// </summary>
/// <example>
/// <code>
/// [Command("!ping", Description = "Test bot responsiveness")]
/// public static void Ping(CommandContext ctx)
/// {
///     ctx.Reply("🏓 Pong!");
/// }
/// </code>
/// </example>
/// <param name="trigger">Command trigger (e.g., "!ping", "!help")</param>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
public sealed class CommandAttribute(string trigger) : Attribute
{
    /// <summary>
    /// Command trigger text (case-insensitive).
    /// </summary>
    public string Trigger { get; } = trigger;

    /// <summary>
    /// Human-readable description shown in help text.
    /// </summary>
    public string Description { get; init; } = "";

    /// <summary>
    /// Restrict command to bot owner only.
    /// </summary>
    public bool OwnerOnly { get; init; } = false;

    /// <summary>
    /// Minimum number of arguments required.
    /// Command fails if user provides fewer arguments.
    /// </summary>
    public int MinArgs { get; init; } = 0;

    /// <summary>
    /// Maximum number of arguments allowed.
    /// -1 means unlimited.
    /// </summary>
    public int MaxArgs { get; init; } = -1;

    /// <summary>
    /// Command cooldown in milliseconds per user.
    /// 0 means no cooldown.
    /// </summary>
    public int CooldownMs { get; init; } = 0;

    /// <summary>
    /// Custom usage hint shown when arguments are invalid.
    /// Example: "!weather [city]"
    /// </summary>
    public string Usage { get; init; } = "";

    /// <summary>
    /// Command aliases (alternative triggers).
    /// </summary>
    public string[] Aliases { get; init; } = [];
}