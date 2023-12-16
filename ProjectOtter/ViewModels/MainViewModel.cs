using System.Collections.ObjectModel;
using System.IO.Compression;
using System.Text.Json;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Xaml;
using ProjectOtter.Helpers;
using Windows.Storage;
using Windows.Storage.Pickers;
using WinRT.Interop;

namespace ProjectOtter.ViewModels;

public partial class MainViewModel : ObservableRecipient
{
    [ObservableProperty]
    private string fileName = "no bug report selected";

    [ObservableProperty]
    private string fileContent = string.Empty;

    [ObservableProperty]
    private ZipArchiveEntry? selectedEntry;

    public List<ZipArchiveEntry> AllZipArchiveEntries { get; set; } = [];

    public ObservableCollection<ZipArchiveEntry> DisplayZipEntries { get; set; } = [];

    [ObservableProperty]
    private bool hideEmptyFiles = true;

    [ObservableProperty]
    private string filterText = string.Empty;

    private readonly DispatcherTimer debounceTimer = new();

    public MainViewModel()
    {
        debounceTimer.Interval = TimeSpan.FromMilliseconds(200);
        debounceTimer.Tick += DebounceTimer_Tick;
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

            if (HideEmptyFiles && entry.Length == 0)
                shouldAdd = false;

            if (!string.IsNullOrEmpty(FilterText) && !entry.FullName.Contains(FilterText, StringComparison.InvariantCultureIgnoreCase))
                shouldAdd = false;

            if (shouldAdd)
                DisplayZipEntries.Add(entry);
        }
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
        foreach(ZipArchiveEntry entry in AllZipArchiveEntries)
            DisplayZipEntries.Add(entry);
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

        ZipArchive zip = ZipFile.OpenRead(file.Path);

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
    }
}
