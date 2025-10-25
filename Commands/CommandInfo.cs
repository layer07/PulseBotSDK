namespace PulseBot.Commands;

/// <summary>
/// Immutable metadata record for a registered bot command.
/// Used for help text generation and command listing.
/// </summary>
/// <param name="Trigger">Command trigger text (e.g., "!ping")</param>
/// <param name="Description">Human-readable description</param>
/// <param name="OwnerOnly">Whether command is restricted to owner</param>
/// <param name="MinArgs">Minimum required arguments</param>
/// <param name="MaxArgs">Maximum allowed arguments (-1 for unlimited)</param>
/// <param name="CooldownMs">Per-user cooldown in milliseconds</param>
/// <param name="Usage">Usage hint (e.g., "!weather [city]")</param>
/// <param name="Aliases">Alternative command triggers</param>
public sealed record CommandInfo(
    string Trigger,
    string Description,
    bool OwnerOnly,
    int MinArgs,
    int MaxArgs,
    int CooldownMs,
    string Usage,
    string[] Aliases
)
{
    /// <summary>
    /// Format command for help text display.
    /// </summary>
    public string FormatHelp() =>
        $"{Trigger}{(OwnerOnly ? " 🔒" : "")} - {Description}";

    /// <summary>
    /// Format detailed usage information.
    /// </summary>
    public string FormatUsage()
    {
        var text = !string.IsNullOrEmpty(Usage) ? Usage : Trigger;

        if (MinArgs > 0)
            text += $" (requires {MinArgs}+ args)";

        if (Aliases.Length > 0)
            text += $" [aliases: {string.Join(", ", Aliases)}]";

        return text;
    }

    /// <summary>
    /// Check if this command matches a given trigger (case-insensitive).
    /// </summary>
    public bool Matches(string trigger)
    {
        var normalized = trigger.ToLowerInvariant();

        if (Trigger.Equals(normalized, StringComparison.OrdinalIgnoreCase))
            return true;

        return Aliases.Any(alias =>
            alias.Equals(normalized, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Get all triggers (primary + aliases).
    /// </summary>
    public IEnumerable<string> GetAllTriggers()
    {
        yield return Trigger;
        foreach (var alias in Aliases)
            yield return alias;
    }
}