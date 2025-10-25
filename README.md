# PulseBot SDK

A C# bot framework for building interactive chat bots with rich UI surfaces, components, and local file serving capabilities.

## Features

- 🤖 **Command System** - Reflection-based command discovery with attributes
- 🎨 **Rich Surfaces** - Spawn desktop-like windows with interactive components
- 📦 **Component Library** - Pre-built UI components (buttons, tables, code blocks, forms, etc.)
- 🔒 **Secure** - HTML sanitization, rate limiting, owner-only commands
- 📁 **File Server** - Built-in HTTP server for serving downloaded files locally
- ⚡ **Real-time** - TCP/UDP/WebSocket transport support
- 🎯 **Type-Safe** - Full C# type safety with strongly-typed DTOs

## Quick Start

### Installation

1. Clone the repository
2. Configure your bot in `app.xml`:

```xml
<?xml version="1.0" encoding="utf-8"?>
<BotConfiguration>
  <Server>
    <IP>localhost</IP>
    <Port>1339</Port>
    <ApiKey>your-api-key-here</ApiKey>
    <Protocol>TCP</Protocol>
  </Server>
  
  <Bot>
    <Name>MyBot</Name>
    <AutoDetectOwner>true</AutoDetectOwner>
  </Bot>
</BotConfiguration>
```

3. Run the bot:

```bash
dotnet run
```

## Creating Commands

Commands are auto-discovered using the `[Command]` attribute:

```csharp
using PulseBot.Commands;

public static class MyCommands
{
    [Command("!ping", Description = "Test bot responsiveness")]
    public static void Ping(CommandContext ctx)
    {
        ctx.Reply("🏓 Pong!");
    }
    
    [Command("!echo", Description = "Echo message", MinArgs = 1)]
    public static void Echo(CommandContext ctx)
    {
        ctx.Reply($"🔊 {ctx.ArgsText}");
    }
}
```

### Command Attributes

- `Trigger` - Command trigger (e.g., `!ping`)
- `Description` - Help text
- `OwnerOnly` - Restrict to bot owner
- `MinArgs` / `MaxArgs` - Argument validation
- `CooldownMs` - Cooldown between uses
- `Aliases` - Alternative triggers

## Bot Surfaces

Spawn interactive UI windows with rich components:

```csharp
[Command("!demo", Description = "Show surface demo")]
public static void Demo(CommandContext ctx)
{
    ctx.Bot.SpawnSurface(new BotSurface
    {
        Title = "Demo Surface",
        Components = new BotComponent[]
        {
            new Heading("Welcome!"),
            new TextBlock("This is a rich UI surface."),
            new CodeBlock("console.log('Hello World');", "javascript"),
            new Table(
                headers: new[] { "Name", "Value" },
                new TableRow(
                    new TableCell("Status"),
                    new TableCell("Online", CellColor.Positive)
                )
            ),
            new ButtonRow(
                new Button("close", "Close")
            )
        }
    });
}
```

### Available Components

- **Text**: `Heading`, `TextBlock`, `KeyValue`
- **Code**: `CodeBlock` (syntax highlighted)
- **Data**: `Table`, `TableRow`, `TableCell`
- **Interactive**: `Button`, `ButtonRow`, `Form`, `Input`, `TextArea`
- **Media**: `ImageEmbed`, `VideoEmbed`, `FileDownload`
- **Layout**: `Divider`, `Embed`, `Status`, `ProgressBar`

### Built-in Actions

Buttons with `data-bot-action` attributes:

- `copy` - Copy text to clipboard
- `download` - Download file
- `close` - Close surface
- Custom actions - Send callback to bot

## Local File Server

Built-in HTTP server for serving downloaded files:

```csharp
// Start file server (in Program.cs)
bot.FileServer = new LocalFileServer(port: 8080);
bot.FileServer.Start();

// Use in commands
[Command("!dl", Description = "Download file", MinArgs = 1)]
public static async void Download(CommandContext ctx)
{
    var url = ctx.GetArg(0);
    var response = await httpClient.GetAsync(url);
    var content = await response.Content.ReadAsByteArrayAsync();
    
    var fileUrl = ctx.Bot.FileServer.SaveFile("file.jpg", content);
    
    ctx.Bot.SpawnSurface(new BotSurface
    {
        Title = "Downloaded",
        Components = new BotComponent[]
        {
            new ImageEmbed(fileUrl),
            new FileDownload(fileUrl, "file.jpg")
        }
    });
}
```

Files are served from `./downloads/` folder at `http://localhost:8080/`.

## Bot Types

### PersonalBot
- Runs on user's own PC
- Commands only processed by owner
- Surfaces spawn for owner only
- Perfect for local automation (file downloads, API calls)

### ServerBot
- Runs on server or shared machine
- Can respond to any server member
- Surfaces targeted to specific users
- Requires shared room validation

## Security

- ✅ **HTML Sanitization** - All surface content sanitized server-side
- ✅ **Rate Limiting** - Max 10 surfaces per minute per bot
- ✅ **Owner-Only** - Commands can be restricted to bot owner
- ✅ **Shared Room Validation** - ServerBots can only target users in accessible rooms
- ✅ **No Inline JS** - Event delegation via `data-bot-action` attributes

## Project Structure

```
pulsebotclient/
├── Commands/
│   ├── CommandAttribute.cs
│   ├── CommandContext.cs
│   └── CommandDispatcher.cs
├── Components/
│   ├── BotComponent.cs
│   ├── TextComponents.cs
│   ├── CodeComponents.cs
│   ├── ButtonComponents.cs
│   └── ...
├── Models/
│   ├── BotMessage.cs
│   ├── BotUser.cs
│   ├── BotSurface.cs
│   └── ...
├── Utilities/
│   └── LocalFileServer.cs
├── ExampleCommands/
│   ├── BasicCommands.cs
│   ├── SurfaceShowcase.cs
│   └── DownloadCommand.cs
└── PulseBot.cs
```

## Built-in Commands

- `!ping` - Test responsiveness
- `!help` - List commands
- `!status` - Bot statistics (owner only)
- `!echo <text>` - Echo message
- `!roll` - Roll dice (1-100)
- `!testsurface` - Surface component showcase
- `!dl <url>` - Download file from URL

## Requirements

- .NET 10+
- MessagePack

## Configuration

Edit `app.xml` to configure:
- Server connection (IP, port, protocol)
- Bot name and owner
- Command settings (rate limits, disabled commands)
- Network settings (timeouts, reconnection)
- Logging options

## License

MIT License - Feel free to use and modify.

## Contributing

Open source SDK - contributions welcome. Add new components, commands, or features via pull requests.

---

**Built with 🔥 and C#**
