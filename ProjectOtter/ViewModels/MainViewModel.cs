using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Xaml;
using ProjectOtter.Contracts.Services;
using ProjectOtter.Helpers;
using ProjectOtter.Models;
using System.Collections.ObjectModel;
using System.IO.Compression;
using System.Text.Json;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.System;
using WinRT.Interop;

namespace ProjectOtter.ViewModels;

public partial class MainViewModel : ObservableRecipient
{
    private OtterFile otterFile = new();
    private string zipPath = string.Empty;
    private ZipArchive? openedZip = null;

    [ObservableProperty]
    private string friendlyName = string.Empty;

    [ObservableProperty]
    private string fileName = "no .zip selected";

    [ObservableProperty]
    private string fileContent = string.Empty;

    [ObservableProperty]
    private ZipArchiveEntry? selectedEntry;

    [ObservableProperty]
    private bool isToolsPaneOpen = false;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(KeyAsVirtualKey))]
    private int keyAsInt = 0;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(GitHubIssueURL))]
    [NotifyPropertyChangedFor(nameof(IssueNumberText))]
    private int gitHubIssueNumber = 1;

    [ObservableProperty]
    private Version powerToysVersion = new(0, 0, 0, 0);

    public Uri GitHubIssueURL => new($"https://github.com/microsoft/PowerToys/issues/{GitHubIssueNumber}");

    public string IssueNumberText => $"Issue #{GitHubIssueNumber}";

    public string KeyAsVirtualKey
    {
        get
        {
            bool canParse = Enum.TryParse(KeyAsInt.ToString(), out VirtualKey vKey);

            if (canParse)
                return vKey.ToString();

            return "Unknown";
        }
    }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(DateTimeFromTimestamp))]
    private int timeStamp = 0;

    public string DateTimeFromTimestamp => DateTimeOffset.FromUnixTimeSeconds(TimeStamp).ToString("yyyy MMM dd ddd HH:mm");

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(WindowsVersion))]
    private int buildNumber = 0;

    public string WindowsVersion
    {
        get
        {
            return BuildNumber switch
            {
                0 => "Windows Version",
                10240 => "Windows 10 version 1507",
                10586 => "Windows 10 version 1511",
                14393 => "Windows 10 version 1607",
                15063 => "Windows 10 version 1703",
                16299 => "Windows 10 version 1709",
                17134 => "Windows 10 version 1803",
                17763 => "Windows 10 version 1809",
                18362 => "Windows 10 version 1903",
                18363 => "Windows 10 version 1909",
                19041 => "Windows 10 version 2004",
                19042 => "Windows 10 version 20H2",
                19043 => "Windows 10 version 21H1",
                19044 => "Windows 10 version 21H2",
                19045 => "Windows 10 version 22H2",
                22000 => "Windows 11 version 21H2",
                22621 => "Windows 11 version 22H2",
                22631 => "Windows 11 version 23H2",
                _ => "unknown",
            };
        }
    }

    public List<ZipArchiveEntry> AllZipArchiveEntries { get; set; } = new();

    public ObservableCollection<ZipArchiveEntry> DisplayZipEntries { get; set; } = new();

    [ObservableProperty]
    private bool hideEmptyFiles = false;

    [ObservableProperty]
    private string filterText = string.Empty;

    private readonly DispatcherTimer debounceTimer = new();
    private readonly DispatcherTimer otterFileDebounceTimer = new();

    public INavigationService NavigationService
    {
        get;
    }

    public MainViewModel(INavigationService navigationService)
    {
        debounceTimer.Interval = TimeSpan.FromMilliseconds(200);
        debounceTimer.Tick += DebounceTimer_Tick;

        otterFileDebounceTimer.Interval = TimeSpan.FromMilliseconds(500);
        otterFileDebounceTimer.Tick += OtterFileDebounceTimer_Tick; ;

        NavigationService = navigationService;
    }

    private void OtterFileDebounceTimer_Tick(object? sender, object e)
    {
        otterFileDebounceTimer.Stop();

        OtterFileValueChanged();
    }

    private void DebounceTimer_Tick(object? sender, object e)
    {
        debounceTimer.Stop();
        DisplayZipEntries.Clear();

        FilterAndHideEntries();
    }

    private void FilterAndHideEntries()
    {
        DisplayZipEntries.Clear();
        if (!HideEmptyFiles && string.IsNullOrWhiteSpace(FilterText))
        {
            ResetCollectionToAll();
            return;
        }

        foreach (ZipArchiveEntry entry in AllZipArchiveEntries)
        {
            bool shouldAdd = true;

            // if (HideEmptyFiles && entry.CompressedLength == 0)
            //     shouldAdd = false;

            if (!string.IsNullOrEmpty(FilterText) && !entry.FullName.Contains(FilterText, StringComparison.InvariantCultureIgnoreCase))
                shouldAdd = false;

            if (shouldAdd)
                DisplayZipEntries.Add(entry);
        }
    }

    private async void OtterFileValueChanged()
    {
        otterFile.FriendlyName = FriendlyName;
        otterFile.GitHubIssueNumber = GitHubIssueNumber;

        if (string.IsNullOrWhiteSpace(zipPath) || openedZip is null)
            return;

        ZipArchiveEntry? entry = openedZip.GetEntry("otterfile.json");
        entry ??= openedZip.CreateEntry("otterfile.json");

        string otterFileAsJson = JsonSerializer.Serialize(otterFile);
        using var stream = entry.Open();
        using StreamWriter writer = new(stream);
        await writer.WriteAsync(otterFileAsJson);
    }

    partial void OnFriendlyNameChanged(string value)
    {
        otterFileDebounceTimer.Stop();
        otterFileDebounceTimer.Start();
    }

    partial void OnGitHubIssueNumberChanged(int value)
    {
        otterFileDebounceTimer.Stop();
        otterFileDebounceTimer.Start();
    }

    partial void OnSelectedEntryChanged(ZipArchiveEntry? value)
    {
        if (value is null)
        {
            FileContent = string.Empty;
            return;
        }

        using var stream = value.Open();
        using var reader = new StreamReader(stream);

        if (Path.GetExtension(value.FullName) == ".json")
        {
            JsonSerializerOptions option = new()
            {
                WriteIndented = true,
            };

            FileContent = JsonSerializer.Serialize(JsonSerializer.Deserialize<JsonElement>(reader.ReadToEnd()), option);
        }
        else
        {
            FileContent = reader.ReadToEnd();
        }
    }

    partial void OnHideEmptyFilesChanged(bool value) => FilterAndHideEntries();

    partial void OnFilterTextChanged(string value)
    {
        debounceTimer.Stop();
        debounceTimer.Start();
    }

    private void ResetCollectionToAll()
    {
        DisplayZipEntries.Clear();
        foreach (ZipArchiveEntry entry in AllZipArchiveEntries)
            DisplayZipEntries.Add(entry);
    }

    [RelayCommand]
    private void FilterOnVersion() => FilterText = PowerToysVersion.ToString();

    [RelayCommand]
    private void ToggleIsPaneOpen() => IsToolsPaneOpen = !IsToolsPaneOpen;

    [RelayCommand]
    private void GoToSettingsPage()
    {
        NavigationService.NavigateTo(typeof(SettingsViewModel).FullName!);
    }

    [RelayCommand]
    private void ResetToHomeText()
    {
        SelectedEntry = null;
        OpenBaselineFiles();
    }

    [RelayCommand]
    private async Task OpenFile()
    {
        FileOpenPicker picker = new()
        {
            ViewMode = PickerViewMode.List,
            SuggestedStartLocation = PickerLocationId.Downloads
        };

        InitializeWithWindow.Initialize(picker, App.MainWindow.GetHandle());

        picker.FileTypeFilter.Add(".zip");

        if (await picker.PickSingleFileAsync() is not StorageFile file)
        {
            FileName = "Operation cancelled.";
            return;
        }

        DisplayZipEntries.Clear();
        AllZipArchiveEntries.Clear();
        FileName = file.Name;

        if (Path.GetExtension(file.Path) != ".zip")
            return;

        zipPath = file.Path;
        ZipArchive zip = ZipFile.Open(zipPath, ZipArchiveMode.Update);

        if (zip.Entries.Count > 0)
        {
            ReadTheZip(zip.Entries);
        }
    }

    private void ReadTheZip(ReadOnlyCollection<ZipArchiveEntry> entries)
    {
        foreach (ZipArchiveEntry entry in entries)
        {
            AllZipArchiveEntries.Add(entry);
        }
        FilterAndHideEntries();

        OpenBaselineFiles();
    }

    private void OpenBaselineFiles()
    {
        FileContent = string.Empty;

        List<string> filesToRead = new()
        {
            "settings.json",
            "UpdateState.json",
            "windows-settings.txt",
            "windows-version.txt",
            "otterfile.json",
        };

        foreach (string file in filesToRead)
        {
            ZipArchiveEntry? entry = AllZipArchiveEntries.FirstOrDefault(x => x.FullName.Equals(file, StringComparison.InvariantCultureIgnoreCase));

            if (file == "otterfile.json")
            {
                if (entry is null)
                {
                    otterFile = new();
                    continue;
                }

                using var otterstream = entry.Open();
                using var otterreader = new StreamReader(otterstream);
                otterFile = JsonSerializer.Deserialize<OtterFile>(otterreader.ReadToEnd()) ?? new();
                FriendlyName = otterFile.FriendlyName;
                GitHubIssueNumber = otterFile.GitHubIssueNumber;
                continue;
            }

            if (entry is null)
                continue;

            FileContent += entry.FullName;
            FileContent += Environment.NewLine;
            using var stream = entry.Open();
            using var reader = new StreamReader(stream);

            if (Path.GetExtension(entry.FullName) == ".json")
            {
                JsonSerializerOptions option = new()
                {
                    WriteIndented = true,
                };

                FileContent += JsonSerializer.Serialize(JsonSerializer.Deserialize<JsonElement>(reader.ReadToEnd()), option);
            }
            else
            {
                FileContent += reader.ReadToEnd();
            }

            FileContent += Environment.NewLine;
            FileContent += Environment.NewLine;
        }

        var fileContentLines = FileContent.Split(Environment.NewLine);
        foreach (var line in fileContentLines)
        {
            if (line.Contains("githubUpdateLastCheckedDate", StringComparison.InvariantCultureIgnoreCase))
            {
                var timeStampString = line.Split(":")[1].Trim();
                // remove quotes and comma
                timeStampString = timeStampString[1..^2];
                if (int.TryParse(timeStampString, out int timeStamp))
                {
                    TimeStamp = timeStamp;
                }
            }
            else if (line.Contains("BuildNumber", StringComparison.InvariantCultureIgnoreCase))
            {
                var buildNumberString = line.Split(":")[1].Trim();
                if (int.TryParse(buildNumberString, out int buildNumber))
                {
                    BuildNumber = buildNumber;
                }
            }
            else if (line.Contains("powertoys_version", StringComparison.InvariantCulture))
            {
                var versionString = line.Split(":")[1].Trim();
                // remove version and comma
                versionString = versionString[2..^1];
                if (Version.TryParse(versionString, out Version? version) && version is not null)
                {
                    PowerToysVersion = version;
                }
            }
        }
    }
}
