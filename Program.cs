using PulseBot.Utilities;

namespace PulseBot;

class Program
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("═══════════════════════════════════════════════════════");
        Console.WriteLine("  PulseBot SDK - Test Application");
        Console.WriteLine("═══════════════════════════════════════════════════════\n");

        // Load config from app.xml
        var bot = PulseBot.FromConfig("app.xml");

        // ✅ START FILE SERVER
        bot.FileServer = new LocalFileServer(port: 8080);
        bot.FileServer.Start();

        // Wire up events
        bot.OnReady += () =>
        {
            Console.WriteLine("\n[READY] Bot is online and ready to receive commands!");
            Console.WriteLine($"[INFO] Servers: {bot.Servers.Count}");
            Console.WriteLine($"[INFO] Stickers: {bot.Stickers.Count}");
            Console.WriteLine($"[INFO] File Server: {bot.FileServer.BaseUrl}");
        };

        bot.OnError += (ex) =>
        {
            Console.WriteLine($"\n[ERROR] {ex.GetType().Name}: {ex.Message}");
        };

        // Start bot
        await bot.Start();

        Console.WriteLine("\n[PULSE] Press CTRL+C to stop\n");

        // Keep running
        await Task.Delay(-1);
    }
}