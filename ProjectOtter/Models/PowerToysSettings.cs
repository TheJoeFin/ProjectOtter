namespace ProjectOtter.Models;

public class PowerToysSettings
{
    public bool startup { get; set; }
    public bool is_elevated { get; set; }
    public bool run_elevated { get; set; }
    public bool download_updates_automatically { get; set; }
    public bool enable_experimentation { get; set; }
    public bool is_admin { get; set; }
    public bool enable_warnings_elevated_apps { get; set; }
    public string theme { get; set; } = string.Empty;
    public string system_theme { get; set; } = string.Empty;
    public Version power_toys_version { get; set; } = new();
    public Dictionary<string, bool> enabled { get; set; } = new();

    public PowerToysSettings()
    {
        
    }
}


/* 
 * {
  "startup": true,
  "enabled": {
    "AlwaysOnTop": true,
    "Awake": true,
    "CmdNotFound": false,
    "ColorPicker": true,
    "CropAndLock": true,
    "EnvironmentVariables": true,
    "FancyZones": true,
    "File Explorer": true,
    "File Locksmith": true,
    "FindMyMouse": true,
    "Hosts": true,
    "Image Resizer": true,
    "Keyboard Manager": false,
    "Measure Tool": true,
    "MouseHighlighter": true,
    "MouseJump": false,
    "MousePointerCrosshairs": false,
    "MouseWithoutBorders": false,
    "PastePlain": true,
    "Peek": true,
    "PowerRename": true,
    "PowerToys Run": true,
    "QuickAccent": false,
    "RegistryPreview": true,
    "Shortcut Guide": true,
    "TextExtractor": true,
    "Video Conference": false
  },
  "is_elevated": false,
  "run_elevated": false,
  "download_updates_automatically": true,
  "enable_experimentation": true,
  "is_admin": true,
  "enable_warnings_elevated_apps": true,
  "theme": "system",
  "system_theme": "dark",
  "powertoys_version": "v0.77.0"
}

 */