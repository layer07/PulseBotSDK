namespace PulseBot.Components;

/// <summary>
/// Code block with optional syntax highlighting.
/// </summary>
public sealed class CodeBlock : BotComponent
{
    public override string Type => "code";

    public string Code { get; }
    public string Language { get; }
    public bool ShowCopyButton { get; }

    public CodeBlock(string code, string language = "text", bool showCopyButton = true)
    {
        Code = code;
        Language = language;
        ShowCopyButton = showCopyButton;
    }

    public override string ToHtml()
    {
        var html = $@"
            <div class='bot-code-block'>
                <div class='bot-code-header'>
                    <span class='bot-code-language'>{Esc(Language)}</span>
                    {(ShowCopyButton ? $"<button data-bot-action='copy' data-content='{Esc(Code)}' class='bot-action-button'>📋 Copy</button>" : "")}
                </div>
                <pre><code class='language-{Esc(Language)}'>{Esc(Code)}</code></pre>
            </div>
        ";
        return html;
    }
}

/// <summary>
/// Inline code snippet.
/// </summary>
public sealed class InlineCode : BotComponent
{
    public override string Type => "inline-code";

    public string Code { get; }

    public InlineCode(string code)
    {
        Code = code;
    }

    public override string ToHtml() =>
        $"<code>{Esc(Code)}</code>";
}