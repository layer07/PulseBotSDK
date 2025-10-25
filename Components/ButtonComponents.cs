namespace PulseBot.Components;

/// <summary>
/// Interactive button with action callback.
/// </summary>
public sealed class Button : BotComponent
{
    public override string Type => "button";

    public string ActionID { get; }
    public string Label { get; }
    public ButtonStyle Style { get; }
    public Dictionary<string, string>? ActionData { get; }

    public Button(string actionId, string label, ButtonStyle style = ButtonStyle.Default, Dictionary<string, string>? actionData = null)
    {
        ActionID = actionId;
        Label = label;
        Style = style;
        ActionData = actionData;
    }

    public override string ToHtml()
    {
        var styleClass = Style switch
        {
            ButtonStyle.Primary => "primary",
            ButtonStyle.Danger => "danger",
            _ => ""
        };

        var dataAttrs = ActionData != null
            ? string.Join(" ", ActionData.Select(kv => $"data-param-{Esc(kv.Key)}=\"{Esc(kv.Value)}\""))
            : "";

        return $"<button data-bot-action='{Esc(ActionID)}' class='bot-action-button {styleClass}' {dataAttrs}>{Esc(Label)}</button>";
    }
}

public enum ButtonStyle
{
    Default,
    Primary,
    Danger
}

/// <summary>
/// Horizontal row of buttons.
/// </summary>
public sealed class ButtonRow : BotComponent
{
    public override string Type => "button-row";

    public Button[] Buttons { get; }

    public ButtonRow(params Button[] buttons)
    {
        Buttons = buttons;
    }

    public override string ToHtml() =>
        $"<div class='bot-button-row'>{string.Join("", Buttons.Select(b => b.ToHtml()))}</div>";
}