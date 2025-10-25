using PulseBot.Commands;
using PulseBot.Components;
using PulseBot.Models;

namespace PulseBot.ExampleCommands;

public static class SurfaceShowcase
{
    [Command("!testsurface", Description = "Showcase all surface components")]
    public static void TestSurface(CommandContext ctx)
    {
        ctx.Bot.SpawnSurface(new BotSurface
        {
            Title = "Component Showcase",
            Components = new BotComponent[]
            {
                new Heading("🎨 Surface Component Gallery"),
                new TextBlock("This surface demonstrates all available components with Corpo Blood styling."),

                new Divider(),

                new Heading("Text Components", 3),
                new TextBlock("Regular paragraph text looks like this."),
                new TextBlock("Bold text for emphasis!", true),
                new KeyValue("Status", "Online"),
                new KeyValue("Server", "Demo Server"),

                new Divider(),

                new Heading("Code Block", 3),
                new CodeBlock(
                    "function greet(name) {\n    console.log(`Hello, ${name}!`);\n    return true;\n}",
                    "javascript"
                ),

                new Divider(),

                new Heading("Data Table", 3),
                new Table(
                    headers: new[] { "Metric", "Value", "Status" },
                    new TableRow(
                        new TableCell("Response Time"),
                        new TableCell("23ms"),
                        new TableCell("✓ Good", CellColor.Positive)
                    ),
                    new TableRow(
                        new TableCell("Memory Usage"),
                        new TableCell("156MB"),
                        new TableCell("○ Normal", CellColor.Neutral)
                    ),
                    new TableRow(
                        new TableCell("Error Rate"),
                        new TableCell("3.2%"),
                        new TableCell("✗ High", CellColor.Negative)
                    )
                ),

                new Divider(),

                new Heading("Status Indicators", 3),
                new Status("Connected", StatusType.Success),
                new TextBlock(" "),
                new Status("Warning: High Load", StatusType.Warning),
                new TextBlock(" "),
                new Status("Database Error", StatusType.Error),
                new TextBlock(" "),
                new Status("Processing...", StatusType.Info),

                new Divider(),

                new Heading("Progress Bar", 3),
                new ProgressBar(75),

                new Divider(),

                new Heading("Embed Card", 3),
                new Embed(
                    title: "📦 GitHub Repository",
                    description: "A powerful bot framework for building interactive chat experiences with rich surfaces and components.",
                    footer: "⭐ 1.2k stars • Updated 2 hours ago"
                ),

                new Divider(),

                new Heading("Interactive Buttons", 3),
                new ButtonRow(
                    new Button("test-action", "🧪 Test Action", ButtonStyle.Primary),
                    new Button("copy", "📋 Copy Text", ButtonStyle.Default, new Dictionary<string, string>
                    {
                        { "content", "This text was copied from the surface!" }
                    }),
                    new Button("close", "❌ Close", ButtonStyle.Danger)
                )
            }
        });

        ctx.Reply("✅ Surface showcase spawned! Check your windows.");
    }

    [Command("!testform", Description = "Test form input surface")]
    public static void TestForm(CommandContext ctx)
    {
        ctx.Bot.SpawnSurface(new BotSurface
        {
            Title = "Form Example",
            Components = new BotComponent[]
            {
                new Heading("📝 Search Form"),
                new TextBlock("Enter a query below and click submit:"),

                new Form(
                    "Search Query",
                    new Button("submit-search", "🔍 Search", ButtonStyle.Primary, new Dictionary<string, string>
                    {
                        { "query", "search-input" }
                    }),
                    new Input("search-input", "Type your query...")
                ),

                new Divider(),

                new TextBlock("Note: Form submission sends data back to bot for processing.", true)
            }
        });

        ctx.Reply("✅ Form surface spawned!");
    }

    [Command("!testvideo", Description = "Test video embed (requires local URL)")]
    public static void TestVideo(CommandContext ctx)
    {
        var videoUrl = ctx.GetArg(0);

        if (string.IsNullOrEmpty(videoUrl))
        {
            ctx.Reply("⚠️ Usage: !testvideo <url>\nExample: !testvideo http://localhost:8080/video.mp4");
            return;
        }

        ctx.Bot.SpawnSurface(new BotSurface
        {
            Title = "Video Player",
            Components = new BotComponent[]
            {
                new Heading("🎥 Video Preview"),
                new VideoEmbed(videoUrl),
                new Divider(),
                new FileDownload(videoUrl, "video.mp4"),
                new ButtonRow(
                    new Button("close", "Close")
                )
            }
        });

        ctx.Reply("✅ Video surface spawned!");
    }
}