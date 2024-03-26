namespace ProjectOtter.Helpers;
public static class LabelHelper
{
    private static readonly Dictionary<string, string> productDictionary = new()
    {
        {"Product-Window Manager", "Window Manager"},
        {"Product-Shortcut Guide", "Shortcut Guide"},
        {"Product-PowerToys Run", "PowerToys Run"},
        {"Product-Keyboard Shortcut Manager", "Keyboard Manager"},
        {"Product-Settings", "Settings"},
        {"FancyZones-Dragging&UI", "FancyZones"},
        {"FancyZones-Editor", "FancyZones"},
        {"FancyZones-Hotkeys", "FancyZones"},
        {"FancyZones-Layouts", "FancyZones"},
        {"FancyZones-Settings", "FancyZones"},
        {"Product-FancyZones", "FancyZones"},
        {"Product-PowerRename", "PowerRename"},
        {"Product-Always On Top", "AlwaysOnTop"},
        {"Product-Virtual Desktop", "Virtual Desktop"},
        {"Product-Power management", "Power management"},
        {"Product-Display management", "Display management"},
        {"Product-Image Resizer", "Image Resizer"},
        {"Product-File Explorer", "File Explorer"},
        {"FancyZones-VirtualDesktop", "VirtualDesktop"},
        {"Run-Plugin", "PowerToys Run"},
        {"Product-Color Picker", "ColorPicker"},
        {"Product-Video Conference Mute", "Video Conference"},
        {"Run-Plugin Manager", "Plugin Manager"},
        {"Product-Awake", "Awake"},
        {"Product-Mouse Utilities", "FindMyMouse,MouseHighlighter,MouseJump,MousePointerCrosshairs"},
        {"Product-Screen Ruler", "Measure Tool"},
        {"Product-Quick Accent", "QuickAccent"},
        {"Product-Text Extractor", "TextExtractor"},
        {"Product-Hosts File Editor", "Hosts"},
        {"Product-File Locksmith", "File Locksmith"},
        {"Product-Peek", "Peek"},
        {"Product-Paste as plain text", "PastePlain"},
        {"Product-Registry Preview", "RegistryPreview"},
        {"Product-Mouse Without Borders", "MouseWithoutBorders"},
        {"Product-CropAndLock", "CropAndLock"},
        {"Product-Environment Variables", "EnvironmentVariables"},
        {"Product-CommandNotFound", "CmdNotFound"},
        {"Product-File Actions Menu", "File Actions Menu"}
    };


    // a method which takes a list of strings as labels and returns a list of products
    public static List<string> GetProducts(List<string> labels)
    {
        List<string> products = new();
        foreach (var label in labels)
            if (productDictionary.TryGetValue(label.Trim(), out string? value))
            {
                if (value.Contains(','))
                {
                    var subList = value.Split(',');
                    foreach (var subValue in subList)
                        products.Add(subValue);
                }
                else
                {
                    products.Add(value);
                }
            }

        return products;
    }
}
