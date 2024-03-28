using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Humanizer;
using Microsoft.UI.Xaml;
using ProjectOtter.Contracts.Services;
using ProjectOtter.Contracts.ViewModels;
using ProjectOtter.Helpers;
using ProjectOtter.Models;
using SimplifiedSearch;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.IO.Compression;
using System.Text.Json;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.System;
using WinRT.Interop;

namespace ProjectOtter.ViewModels;

public partial class MainViewModel : ObservableRecipient, INavigationAware
{
    private string otterFileName = "otterFile.json";
    private OtterFile? otterFile = new();
    private string zipPath = string.Empty;
    private bool loadingSettingsFile = false;
    private bool hasGhCli = false;
    private bool isWrittingToOtterFile = false;
    private ZipArchive? zip = null;

    [ObservableProperty]
    private DateTime? bugReportDateTime;

    public string HowLongAgoBugReport => BugReportDateTime?.Humanize() ?? "unknown";

    [ObservableProperty]
    private string friendlyName = string.Empty;

    [ObservableProperty]
    private string fileName = "no .zip selected";

    [ObservableProperty]
    private string fileContent = string.Empty;

    [ObservableProperty]
    private ZipEntryItem? selectedEntry;

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
    private string startingText = string.Empty;

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

    public ObservableCollection<PreviousItem> PreviousItems { get; set; } = new();

    [ObservableProperty]
    private PreviousItem? selectedPreviousItem;

    [ObservableProperty]
    private bool filterOutOldLogs = true;

    public ObservableCollection<UtilityFilter> UtilitiesFilter { get; set; } = new();

    public List<ZipEntryItem> AllZipArchiveEntries { get; set; } = new();

    public ObservableCollection<ZipEntryItem> DisplayZipEntries { get; set; } = new();

    [ObservableProperty]
    private bool showFailedToReadFile = false;

    [ObservableProperty]
    private bool filterOnUtility = true;

    [ObservableProperty]
    private string errorMessage = string.Empty;

    [ObservableProperty]
    private string errorTitle = "Error opening file";

    [ObservableProperty]
    private bool hideEmptyFiles = true;

    [ObservableProperty]
    private string filterText = string.Empty;

    private readonly DispatcherTimer debounceTimer = new();
    private readonly DispatcherTimer otterFileDebounceTimer = new();
    private readonly DispatcherTimer renameFileClosedTimer = new();

    public INavigationService NavigationService { get; }

    public ILocalSettingsService LocalSettingsService { get; }

    public MainViewModel(INavigationService navigationService, ILocalSettingsService localSettingsService)
    {
        debounceTimer.Interval = TimeSpan.FromMilliseconds(200);
        debounceTimer.Tick += DebounceTimer_Tick;

        otterFileDebounceTimer.Interval = TimeSpan.FromMilliseconds(500);
        otterFileDebounceTimer.Tick += OtterFileDebounceTimer_Tick;

        renameFileClosedTimer.Interval = TimeSpan.FromMilliseconds(1000);
        renameFileClosedTimer.Tick += async (s, e) =>
        {
            renameFileClosedTimer.Stop();
            await CheckIsIssueClosed();
        };

        NavigationService = navigationService;
        LocalSettingsService = localSettingsService;
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

    partial void OnFilterOnUtilityChanged(bool value)
    {
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

        foreach (ZipEntryItem entry in AllZipArchiveEntries)
        {
            bool shouldAdd = true;

            bool isOld = false;

            if (FilterOutOldLogs && BugReportDateTime is not null && entry.CreationDate is not null)
            {
                double totalDaysFromBugReport = (BugReportDateTime.Value - entry.CreationDate.Value).TotalDays;

                if (totalDaysFromBugReport < 7)
                    isOld = true;
            }
            if (FilterOnUtility )
            {
                bool isInFilter = false;

                var enabledUtilityFilters = UtilitiesFilter.Where(x => x.IsFiltering);

                foreach (UtilityFilter utilityFilter in enabledUtilityFilters)
                {
                    if (!utilityFilter.IsFiltering)
                        continue;

                    if (entry.Entry.FullName.Contains(utilityFilter.UtilityName, StringComparison.InvariantCultureIgnoreCase))
                    {
                        isInFilter = true;
                        break;
                    }
                }

                if (!enabledUtilityFilters.Any())
                    isInFilter = true;

                shouldAdd = isInFilter;
            }

            if (FilterOutOldLogs && isOld && FilterOnUtility)
                shouldAdd = false;

            if (HideEmptyFiles && entry.IsEmpty)
                shouldAdd = false;

            if (!string.IsNullOrEmpty(FilterText) && !entry.Entry.FullName.Contains(FilterText, StringComparison.InvariantCultureIgnoreCase))
                shouldAdd = false;


            if (shouldAdd)
                DisplayZipEntries.Add(entry);
        }
    }

    private async Task OtterFileValueChanged()
    {
        if (isWrittingToOtterFile)
            return;

        int randomId = Random.Shared.Next(1, 1000);
        Debug.WriteLine($"{randomId}:id: OtterFileValueChanged, writing updates to OtterFile");
        otterFileDebounceTimer.Stop();

        if (string.IsNullOrWhiteSpace(zipPath))
        {
            Debug.WriteLine($"{randomId}:id: zipPath is empty, not writing to otterFile");
            return;
        }

        isWrittingToOtterFile = true;
        otterFile ??= new();
        otterFile.FriendlyName = FriendlyName;
        otterFile.GitHubIssueNumber = GitHubIssueNumber;

        List<string> utilitiesWithFilteringOn = UtilitiesFilter
            .Where(f => f.IsFiltering == true)
            .Select(filter => filter.UtilityName)
            .ToList();

        otterFile.RelatedUtilities = utilitiesWithFilteringOn;
        otterFile.FilteringText = FilterText;

        if (string.IsNullOrWhiteSpace(zipPath))
            return;

        try
        {
            using ZipArchive zip = ZipFile.Open(zipPath, ZipArchiveMode.Update);
            ZipArchiveEntry? entry = zip.GetEntry(otterFileName);
            entry ??= zip.CreateEntry(otterFileName);
            entry.Delete();
            entry = zip.CreateEntry(otterFileName);

            string otterFileAsJson = JsonSerializer.Serialize(otterFile);
            using var stream = entry.Open();
            using StreamWriter writer = new(stream);
            await writer.WriteAsync(otterFileAsJson);
            await SaveCurrentItemToHistory();
        }
        catch (IOException)
        {
            Debug.WriteLine($"{randomId}:id: Failed to write to otterFile.json, trying again");
            otterFileDebounceTimer.Stop();
            otterFileDebounceTimer.Start();
        }
        finally
        {
            isWrittingToOtterFile = false;
            Debug.WriteLine($"{randomId}:id: writing updates to OtterFile done");
        }
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

    partial void OnSelectedPreviousItemChanged(PreviousItem? value)
    {
        if (value is null || Path.GetExtension(value.ZipPath) != ".zip")
            return;

        TryToOpenThisPath(value.ZipPath);

        SelectedPreviousItem = null;
    }

    partial void OnSelectedEntryChanged(ZipEntryItem? value)
    {
        if (value is null)
        {
            FileContent = string.Empty;
            return;
        }
        using ZipArchive zip = ZipFile.Open(zipPath, ZipArchiveMode.Read);
        ZipArchiveEntry? entry = zip.GetEntry(value.Entry.FullName);

        if (entry is null)
            return;

        using var stream = entry.Open();
        using var reader = new StreamReader(stream);

        if (value.IsJSON)
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

        otterFileDebounceTimer.Stop();
        otterFileDebounceTimer.Start();
    }

    private void ResetCollectionToAll()
    {
        DisplayZipEntries.Clear();
        foreach (ZipEntryItem entry in AllZipArchiveEntries)
            DisplayZipEntries.Add(entry);
    }

    [RelayCommand]
    private async Task TryToGetGitHubDetails()
    {
        await GetGitHubDetails();
    }

    [RelayCommand]
    private void CloseOpenedFile() => CloseZip();

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
    private async Task ResetToHomeText()
    {
        SelectedEntry = null;
        await OpenBaselineFiles();
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
            FileName = ".zip opening cancelled.";
            return;
        }

        await TryToOpenThisPath(file.Path);
    }

    private async Task TryToOpenThisPath(string path)
    {
        ShowFailedToReadFile = false;
        CloseZip();

        FileName = Path.GetFileNameWithoutExtension(path);

        if (Path.GetExtension(path) != ".zip")
        {
            ShowFailedToReadFile = true;
            return;
        }

        zipPath = path;

        // parse file name (ex: PowerToysReport_2024-03-23-12-29-28.zip) into a date time
        // substring(14) removes the "PowerToysReport_" part
        string datePart = FileName
                            .Replace("PowerToysReport_", "")
                            .Replace(".zip", "")
                            .Replace("CLOSED_", "");
        if (DateTime.TryParseExact(datePart, "yyyy-MM-dd-HH-mm-ss", null, DateTimeStyles.None, out DateTime dateTime))
        {
            BugReportDateTime = dateTime;
            FileName += $" ({BugReportDateTime.Humanize()})";
        }

        try
        {
            zip = ZipFile.Open(zipPath, ZipArchiveMode.Read);

            if (zip.Entries.Count > 0)
            {
                await ReadTheZip(zip.Entries);
            }
        }
        catch (NotSupportedException notSupportedException)
        {
            ErrorTitle = "File format not supported";
            ErrorMessage = notSupportedException.Message;
            ShowFailedToReadFile = true;
        }
        catch (DirectoryNotFoundException directoryNotFoundException)
        {
            ErrorTitle = "Directory not found";
            ErrorMessage = directoryNotFoundException.Message;
            ShowFailedToReadFile = true;
        }
        catch (FileNotFoundException fileNotFoundException)
        {
            ErrorTitle = "File not found";
            ErrorMessage = fileNotFoundException.Message;
            ShowFailedToReadFile = true;
        }
        catch (Exception ex)
        {
            ErrorTitle = "Error opening file";
            ErrorMessage = ex.Message;
            ShowFailedToReadFile = true;
        }
        finally
        {
            zip?.Dispose();
        }
    }

    private async Task ReadTheZip(ReadOnlyCollection<ZipArchiveEntry> entries)
    {
        foreach (ZipArchiveEntry entry in entries)
        {
            ZipEntryItem zipEntry = new(entry);
            AllZipArchiveEntries.Add(zipEntry);
        }

        await OpenBaselineFiles();
        FilterAndHideEntries();
        await SaveCurrentItemToHistory();
    }

    private async Task OpenBaselineFiles()
    {
        FileContent = string.Empty;

        List<string> filesToRead = new()
        {
            "otterFile.json",
            "settings.json",
            "UpdateState.json",
            "windows-settings.txt",
            "windows-version.txt",
        };

        bool readOtterFile = false;

        foreach (string file in filesToRead)
        {
            ZipEntryItem? entry = AllZipArchiveEntries.FirstOrDefault(x => x.Entry.FullName.Equals(file, StringComparison.InvariantCultureIgnoreCase));

            if (file == "otterFile.json")
            {
                if (entry is null)
                {
                    otterFile = new();
                    continue;
                }

                using var otterStream = entry.Entry.Open();
                using var otterReader = new StreamReader(otterStream);
                string otterContents = otterReader.ReadToEnd();

                try
                {
                    otterFile = JsonSerializer.Deserialize<OtterFile>(otterContents) ?? new();
                    FriendlyName = otterFile.FriendlyName;
                    GitHubIssueNumber = otterFile.GitHubIssueNumber;
                    FilterText = otterFile.FilteringText;
                    readOtterFile = true;
                }
                catch
                {
                    Debug.WriteLine("Failed to read otterFile");
                }
                continue;
            }

            if (entry is null)
                continue;

            if (file == "settings.json")
            {
                try
                {
                    ReadSettingsFile(entry);
                }
                catch(Exception e)
                {
                    FileContent += "Failed to read settings.json";
                    FileContent += Environment.NewLine;
                    FileContent += e.Message;
                }
            }

            FileContent += entry.Entry.FullName;
            FileContent += Environment.NewLine;
            using var stream = entry.Entry.Open();
            using var reader = new StreamReader(stream);

            if (Path.GetExtension(entry.Entry.FullName) == ".json")
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

        StartingText = FileContent;

        if (!readOtterFile && hasGhCli)
        {
            Debug.WriteLine("No otter file found, trying to get GitHub details");
            await GetGitHubDetails();
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

        if (GitHubIssueNumber != 1 && hasGhCli)
            await CheckIsIssueClosed();
    }

    private async Task CheckIsIssueClosed()
    {
        bool isClosed = await GitHubCliHelper.IsIssueClosed(GitHubIssueNumber);

        if (!isClosed)
            return;

        if (FileName.StartsWith("CLOSED", StringComparison.InvariantCultureIgnoreCase))
            return;

        // rename zip file to include CLOSED before the PowerToysReport part
        string newZipPath = zipPath.Replace("PowerToysReport_", "CLOSED_PowerToysReport_");

        try
        {
            File.Move(zipPath, newZipPath);
            zipPath = newZipPath;
            FileName = $"CLOSED: {FileName}";
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to rename file with closed, trying again: {ex.Message}");
            renameFileClosedTimer.Start();
        }
    }

    private async Task GetGitHubDetails()
    {
        GitHubResponse? cliResponse = await GitHubCliHelper.GetIssueDetails(Path.GetFileNameWithoutExtension(zipPath));

        if (cliResponse is null)
        {
            Debug.WriteLine("Failed to get GitHub details from CLI");
            return;
        }

        FriendlyName = cliResponse.Title;
        GitHubIssueNumber = cliResponse.IssueNumber;

        if (UtilitiesFilter.Where(x => x.IsFiltering).Count() <= 1)
        {
            // look at the labels on the GitHub issue and add them to the filter
            List<string> productFilters = LabelHelper.GetProducts(cliResponse.Labels);
            foreach (string product in productFilters)
            {
                UtilityFilter? filter = UtilitiesFilter.FirstOrDefault(x => x.UtilityName == product);
                if (filter != null)
                    filter.IsFiltering = true;
            }

            FilterOnUtility = true;
        }

        Debug.WriteLine($"Got GitHub details from CLI: {cliResponse.Title} #{cliResponse.IssueNumber}");
    }

    private void ReadSettingsFile(ZipEntryItem entry)
    {
        if (loadingSettingsFile)
            return;

        loadingSettingsFile = true; 

        using var stream = entry.Entry.Open();
        using var reader = new StreamReader(stream);
        string content = reader.ReadToEnd();

        PowerToysSettings? settings = JsonSerializer.Deserialize<PowerToysSettings>(content);

        if (settings is null || otterFile is null)
            return;

        foreach (var utility in settings.enabled)
        {
            UtilityFilter utilityFilter = new()
            {
                UtilityName = utility.Key,
                IsFiltering = false,
            };

            bool isListedOnOtterFile = otterFile.RelatedUtilities.Contains(utility.Key);
            utilityFilter.IsFiltering = isListedOnOtterFile;

            utilityFilter.FilteringChanged += UtilityFilter_FilteringChanged;
            UtilitiesFilter.Add(utilityFilter);
        }

        if (otterFile.RelatedUtilities.Any())
            FilterOnUtility = true;

        loadingSettingsFile = false;
    }

    private void CloseZip()
    {
        zipPath = string.Empty;
        otterFile = null;
        FileContent = string.Empty;
        FriendlyName = string.Empty;
        SelectedEntry = null;
        GitHubIssueNumber = 1;
        FilterOnUtility = false;
        AllZipArchiveEntries.Clear();
        UtilitiesFilter.Clear();
        FileName = "no .zip selected";
        FilterAndHideEntries();
    }

    private void UtilityFilter_FilteringChanged(object? sender, EventArgs e)
    {
        if (!FilterOnUtility)
            return;

        debounceTimer.Stop();
        debounceTimer.Start();

        otterFileDebounceTimer.Stop();
        otterFileDebounceTimer.Start();
    }

    public async void OnNavigatedTo(object parameter)
    {
        await LoadPreviousItems();
        hasGhCli = await GitHubCliHelper.IsGhCliInstalled();
    }

    public void OnNavigatedFrom()
    {
    }

    private async Task LoadPreviousItems()
    {
        ICollection<PreviousItem>? prevHistory = null;

        try
        {
            prevHistory = await LocalSettingsService.ReadSettingAsync<ICollection<PreviousItem>>(nameof(PreviousItems));
        }
        catch { }

        if (prevHistory is null || prevHistory.Count == 0)
            return;

        foreach (PreviousItem hisItem in prevHistory)
            PreviousItems.Add(hisItem);
    }

    private async Task SaveCurrentItemToHistory()
    {
        if (string.IsNullOrWhiteSpace(zipPath))
            return;

        string displayText = @"// no GitHub number or friendly name set";
        
        if (GitHubIssueNumber != 1)
            displayText = $"#{GitHubIssueNumber} {FriendlyName}";

        PreviousItem previousItem = new()
        {
            ZipPath = zipPath,
            DisplayText = displayText,
        };

        if (PreviousItems.Contains(previousItem))
            PreviousItems.Remove(previousItem);
        PreviousItems.Insert(0, previousItem);

        await LocalSettingsService.SaveSettingAsync(nameof(PreviousItems), PreviousItems);
    }
}
