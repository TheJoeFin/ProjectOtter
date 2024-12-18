namespace ProjectOtter.Models;

public class KeyRemappings
{
    public required Remapkeys remapKeys { get; set; }
    public required string remapKeysToText { get; set; }
    public required Remapshortcuts remapShortcuts { get; set; }
    public required string remapShortcutsToText { get; set; }
}

public class Remapkeys
{
    public required Inprocess[] inProcess { get; set; }
}

public class Inprocess
{
    public required string originalKeys { get; set; }
    public required string newRemapKeys { get; set; }
}

public class Remapshortcuts
{
    public required Shortcut[] global { get; set; }
    public required object[] appSpecific { get; set; }
}

public class Shortcut
{
    public required string originalKeys { get; set; }
    public required int operationType { get; set; }
    public required int secondKeyOfChord { get; set; }
    public required string newRemapKeys { get; set; }
}