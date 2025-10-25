using System.Collections.Concurrent;
using System.Reflection;
using PulseBot.Models;

namespace PulseBot.Commands;

/// <summary>
/// Reflection-based command dispatcher with automatic discovery.
/// Scans assemblies for [Command] attributes and routes messages to handlers.
/// </summary>
public sealed class CommandDispatcher
{
    private readonly PulseBot _bot;
    private readonly ConcurrentDictionary<string, CommandHandler> _commands = new();
    private readonly ConcurrentDictionary<string, ConcurrentDictionary<Guid, DateTime>> _cooldowns = new();

    /// <summary>
    /// Command handler delegate with execution logic.
    /// </summary>
    private sealed record CommandHandler(
        CommandInfo Info,
        Action<CommandContext> Execute
    );

    /// <summary>
    /// Initialize dispatcher and discover commands from all loaded assemblies.
    /// </summary>
    public CommandDispatcher(PulseBot bot)
    {
        _bot = bot;
        DiscoverCommands();
    }

    /// <summary>
    /// Discover all methods marked with [Command] attribute via reflection.
    /// </summary>
    private void DiscoverCommands()
    {
        var assemblies = AppDomain.CurrentDomain.GetAssemblies();
        var commandCount = 0;

        foreach (var assembly in assemblies)
        {
            var methods = assembly.GetTypes()
                .SelectMany(t => t.GetMethods(BindingFlags.Public | BindingFlags.Static))
                .Where(m => m.GetCustomAttribute<CommandAttribute>() is not null);

            foreach (var method in methods)
            {
                var attr = method.GetCustomAttribute<CommandAttribute>()!;

                // Validate method signature
                var parameters = method.GetParameters();
                if (parameters.Length != 1 || parameters[0].ParameterType != typeof(CommandContext))
                {
                    Console.WriteLine($"[COMMAND] ❌ Invalid signature: {method.Name} (expected: void Method(CommandContext ctx))");
                    continue;
                }

                // Create handler delegate
                var handler = new CommandHandler(
                    Info: new CommandInfo(
                        Trigger: attr.Trigger.ToLowerInvariant(),
                        Description: attr.Description,
                        OwnerOnly: attr.OwnerOnly,
                        MinArgs: attr.MinArgs,
                        MaxArgs: attr.MaxArgs,
                        CooldownMs: attr.CooldownMs,
                        Usage: attr.Usage,
                        Aliases: attr.Aliases.Select(a => a.ToLowerInvariant()).ToArray()
                    ),
                    Execute: (Action<CommandContext>)Delegate.CreateDelegate(
                        typeof(Action<CommandContext>),
                        method
                    )
                );

                // Register primary trigger
                _commands[handler.Info.Trigger] = handler;
                commandCount++;

                // Register aliases
                foreach (var alias in handler.Info.Aliases)
                {
                    _commands[alias] = handler;
                }

                Console.WriteLine($"[COMMAND] Registered: {attr.Trigger} - {attr.Description}");
            }
        }

        Console.WriteLine($"[DISPATCHER] Discovered {commandCount} commands");
    }

    /// <summary>
    /// Dispatch incoming message to appropriate command handler.
    /// </summary>
    public void Dispatch(BotMessage message)
    {
        // Only process messages starting with command prefix
        if (!message.Content.StartsWith('!')) return;

        // Parse command and arguments
        var parts = message.Content.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var trigger = parts[0].ToLowerInvariant();
        var args = parts.Length > 1 ? parts.Skip(1).ToArray() : Array.Empty<string>();

        // Find handler
        if (!_commands.TryGetValue(trigger, out var handler))
        {
            _bot.ReplyToMessage(
                message.RoomID,
                message.MessageID,
                $"❓ Unknown command: {trigger}\nTry !help for available commands"
            );
            return;
        }

        var context = new CommandContext(_bot, message, args);

        // Check owner-only restriction
        if (handler.Info.OwnerOnly && !context.IsOwner)
        {
            context.Reply("🔒 This command is restricted to the bot owner");
            return;
        }

        // Check minimum arguments
        if (args.Length < handler.Info.MinArgs)
        {
            var usage = !string.IsNullOrEmpty(handler.Info.Usage)
                ? handler.Info.Usage
                : $"{trigger} requires at least {handler.Info.MinArgs} argument(s)";

            context.Reply($"⚠️ Usage: {usage}");
            return;
        }

        // Check maximum arguments
        if (handler.Info.MaxArgs >= 0 && args.Length > handler.Info.MaxArgs)
        {
            context.Reply($"⚠️ Too many arguments. Maximum: {handler.Info.MaxArgs}");
            return;
        }

        // Check cooldown
        if (handler.Info.CooldownMs > 0)
        {
            var userCooldowns = _cooldowns.GetOrAdd(trigger, _ => new());

            if (userCooldowns.TryGetValue(message.AuthorPublicKey, out var lastUsed))
            {
                var elapsed = DateTime.UtcNow - lastUsed;
                var remaining = TimeSpan.FromMilliseconds(handler.Info.CooldownMs) - elapsed;

                if (remaining > TimeSpan.Zero)
                {
                    context.Reply($"⏳ Cooldown active. Wait {remaining.TotalSeconds:F1}s");
                    return;
                }
            }

            userCooldowns[message.AuthorPublicKey] = DateTime.UtcNow;
        }

        // Execute command
        try
        {
            handler.Execute(context);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[COMMAND ERROR] {trigger}: {ex.InnerException?.Message ?? ex.Message}");
            Console.WriteLine(ex.StackTrace);

            context.Reply($"❌ Command failed: {ex.InnerException?.Message ?? ex.Message}");
        }
    }

    /// <summary>
    /// Get all registered commands (excluding aliases).
    /// </summary>
    public IEnumerable<CommandInfo> GetCommands() =>
        _commands.Values
            .Select(h => h.Info)
            .DistinctBy(info => info.Trigger)
            .OrderBy(info => info.Trigger);

    /// <summary>
    /// Get command by trigger (case-insensitive).
    /// </summary>
    public CommandInfo? GetCommand(string trigger) =>
        _commands.TryGetValue(trigger.ToLowerInvariant(), out var handler)
            ? handler.Info
            : null;

    /// <summary>
    /// Check if a command exists.
    /// </summary>
    public bool HasCommand(string trigger) =>
        _commands.ContainsKey(trigger.ToLowerInvariant());

    /// <summary>
    /// Clear all cooldowns for a specific user.
    /// </summary>
    public void ClearCooldowns(Guid userPublicKey)
    {
        foreach (var cooldownDict in _cooldowns.Values)
        {
            cooldownDict.TryRemove(userPublicKey, out _);
        }
    }

    /// <summary>
    /// Clear all cooldowns for all users.
    /// </summary>
    public void ClearAllCooldowns() => _cooldowns.Clear();
}