using PulseBot.Commands;

namespace PulseBot.Commands;

/// <summary>
/// Basic example commands for testing.
/// </summary>
public static class BasicCommands
{
    private static readonly Random Random = Random.Shared;

    [Command("!ping", Description = "Test bot responsiveness")]
    public static void Ping(CommandContext ctx)
    {
        ctx.Reply("🏓 Pong!");
    }

    [Command("!react", Description = "React to message with random sticker")]
    public static async void React(CommandContext ctx)
    {
        var sticker = ctx.GetRandomSticker();

        if (sticker is null)
        {
            ctx.Reply("⚠️ No stickers available yet");
            return;
        }

        Console.WriteLine($"[REACT] Adding sticker '{sticker.Name}' to message {ctx.Message.MessageID}");
        await ctx.React(sticker.StickerUUID);
    }

    [Command("!stickers", Description = "List available stickers")]
    public static void Stickers(CommandContext ctx)
    {
        if (ctx.Bot.Stickers.Count == 0)
        {
            ctx.Reply("⚠️ No stickers loaded yet");
            return;
        }

        var stickerList = "🎨 Available Stickers:\n" +
                         string.Join("\n", ctx.Bot.Stickers.Take(10).Select(s => $"- {s.Name} ({s.Scope})"));

        if (ctx.Bot.Stickers.Count > 10)
        {
            stickerList += $"\n... and {ctx.Bot.Stickers.Count - 10} more";
        }

        ctx.Reply(stickerList);
    }

    [Command("!help", Description = "Show available commands")]
    public static void Help(CommandContext ctx)
    {
        var commands = ctx.Bot.Dispatcher.GetCommands();

        var help = "📖 Available Commands:\n" +
                   string.Join("\n", commands
                       .Where(c => !c.OwnerOnly || ctx.IsOwner)
                       .Select(c => c.FormatHelp()));

        ctx.Reply(help);
    }

    [Command("!status", Description = "Show bot statistics", OwnerOnly = true)]
    public static void Status(CommandContext ctx)
    {
        var uptime = DateTime.UtcNow - ctx.Bot.StartTime;

        var status = $"📊 Bot Status:\n" +
                    $"- Username: {ctx.Bot.Me?.Username}\n" +
                    $"- Servers: {ctx.Bot.Servers.Count}\n" +
                    $"- Messages seen: {ctx.Bot.Messages.Count}\n" +
                    $"- Stickers: {ctx.Bot.Stickers.Count}\n" +
                    $"- Uptime: {uptime:hh\\:mm\\:ss}";

        ctx.Reply(status);
    }

    [Command("!echo", Description = "Echo back your message", MinArgs = 1)]
    public static void Echo(CommandContext ctx)
    {
        ctx.Reply($"🔊 {ctx.ArgsText}");
    }

    [Command("!roll", Description = "Roll a dice (1-100)")]
    public static void Roll(CommandContext ctx)
    {
        var number = Random.Next(1, 101);
        ctx.Reply($"🎲 You rolled: **{number}**");
    }
}