using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using ProjectOtter.Models;
using System.Text;
using System.Text.Json;
using Windows.System;


namespace ProjectOtter.Controls;

public sealed partial class ParseKeys : UserControl
{
    private readonly JsonSerializerOptions options = new()
    {
        AllowTrailingCommas = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingDefault,
        PropertyNameCaseInsensitive = true
    };

    public string EntryText
    {
        get { return (string)GetValue(EntryTextProperty); }
        set { SetValue(EntryTextProperty, value); }
    }

    public static readonly DependencyProperty EntryTextProperty =
        DependencyProperty.Register("EntryText", typeof(string), typeof(ParseKeys), new PropertyMetadata(string.Empty));

    private readonly DispatcherTimer timer = new();

    public ParseKeys()
    {
        InitializeComponent();

        timer.Interval = TimeSpan.FromMilliseconds(500);
        timer.Tick += Timer_Tick;
    }

    private void Timer_Tick(object? sender, object e)
    {
        timer.Stop();
        ParseText();

    }

    private void InputKeysTextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        timer.Stop();
        timer.Start();
    }

    private void ParseText()
    {
        // get json and parse it out
        string json = InputKeysTextBox.Text;

        if (string.IsNullOrWhiteSpace(json))
        {
            ParsedKeysTextBlock.Text = string.Empty;
            return;
        }

        KeyRemappings? keys = null;

        try
        {
            keys = JsonSerializer.Deserialize<KeyRemappings>(json, options);
        }
        catch (Exception ex)
        {
            ParsedKeysTextBlock.Text = ex.Message;
        }

        if (keys is null)
        {
            ParsedKeysTextBlock.Text = "No JSON";
            return;
        }

        StringBuilder sb = new();
        if (keys.remapKeys.inProcess.Length == 0)
            sb.AppendLine("No remapped keys");
        else
            sb.AppendLine("Remapped Keys");
        sb.AppendLine();

        foreach (var key in keys.remapKeys.inProcess)
        {
            string[] originalKeys = key.originalKeys.Split(';');
            string[] newRemapKeys = key.newRemapKeys.Split(';');

            foreach ( var originalKey in originalKeys)
            {
                bool canParse = Enum.TryParse(originalKey, out VirtualKey vKey);

                if (canParse)
                    sb.AppendLine($"{originalKey}:\t{vKey}");
                else
                    sb.AppendLine($"{originalKey}:\tUnknown");
            }

            sb.AppendLine("map to:");

            foreach (var newRemapKey in newRemapKeys)
            {
                bool canParse = Enum.TryParse(newRemapKey, out VirtualKey vKey);
                if (canParse)
                    sb.AppendLine($"{newRemapKey}:\t{vKey}");
                else
                    sb.AppendLine($"{newRemapKey}:\tUnknown");
            }

            sb.AppendLine();
        }

        sb.AppendLine();
        if (keys.remapShortcuts.global.Length == 0)
            sb.AppendLine("No remapped shortcuts");
        else
            sb.AppendLine("Remapped Shortcuts");
        sb.AppendLine();

        foreach (Shortcut shortcut in keys.remapShortcuts.global)
        {
            string[] originalKeys = shortcut.originalKeys.Split(';');
            string[] newRemapKeys = shortcut.newRemapKeys.Split(';');

            foreach (var originalKey in originalKeys)
            {
                bool canParse = Enum.TryParse(originalKey, out VirtualKey vKey);

                if (canParse)
                    sb.AppendLine($"{originalKey}:\t{vKey}");
                else
                    sb.AppendLine($"{originalKey}:\tUnknown");
            }

            sb.AppendLine("map to:");

            foreach (var newRemapKey in newRemapKeys)
            {
                bool canParse = Enum.TryParse(newRemapKey, out VirtualKey vKey);
                if (canParse)
                    sb.AppendLine($"{newRemapKey}:\t{vKey}");
                else
                    sb.AppendLine($"{newRemapKey}:\tUnknown");
            }

            if (shortcut.secondKeyOfChord != 0)
            {
                sb.AppendLine();
                sb.AppendLine("second key of chord:");
                sb.AppendLine(shortcut.secondKeyOfChord.ToString());

                bool canParse = Enum.TryParse(shortcut.secondKeyOfChord.ToString(), out VirtualKey vKey);
                if (canParse)
                    sb.AppendLine($"{shortcut.secondKeyOfChord.ToString()}:\t{vKey}");
                else
                    sb.AppendLine($"{shortcut.secondKeyOfChord.ToString()}:\tUnknown");
            }

            sb.AppendLine();
        }

        ParsedKeysTextBlock.Text = sb.ToString();
    }
}
