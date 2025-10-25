namespace PulseBot.Components;

/// <summary>
/// Text input field.
/// </summary>
public sealed class Input : BotComponent
{
    public override string Type => "input";

    public string Id { get; }
    public string Placeholder { get; }

    public Input(string id, string placeholder = "")
    {
        Id = id;
        Placeholder = placeholder;
    }

    public override string ToHtml() =>
        $"<input type='text' id='{Esc(Id)}' placeholder='{Esc(Placeholder)}' class='bot-input'>";
}

/// <summary>
/// Multi-line text area.
/// </summary>
public sealed class TextArea : BotComponent
{
    public override string Type => "textarea";

    public string Id { get; }
    public string Placeholder { get; }
    public int Rows { get; }

    public TextArea(string id, string placeholder = "", int rows = 4)
    {
        Id = id;
        Placeholder = placeholder;
        Rows = rows;
    }

    public override string ToHtml() =>
        $"<textarea id='{Esc(Id)}' placeholder='{Esc(Placeholder)}' rows='{Rows}' class='bot-textarea'></textarea>";
}

/// <summary>
/// Form with inputs and submit button.
/// </summary>
public sealed class Form : BotComponent
{
    public override string Type => "form";

    public string Label { get; }
    public BotComponent[] Fields { get; }
    public Button SubmitButton { get; }

    public Form(string label, Button submitButton, params BotComponent[] fields)
    {
        Label = label;
        Fields = fields;
        SubmitButton = submitButton;
    }

    public override string ToHtml()
    {
        var fieldsHtml = string.Join("", Fields.Select(f => $"<div class='bot-form-group'>{f.ToHtml()}</div>"));

        return $@"
            <div class='bot-form'>
                <label class='bot-form-label'>{Esc(Label)}</label>
                {fieldsHtml}
                {SubmitButton.ToHtml()}
            </div>
        ";
    }
}