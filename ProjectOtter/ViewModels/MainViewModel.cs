﻿using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Xaml;
using ProjectOtter.Contracts.Services;
using ProjectOtter.Helpers;
using System.Collections.ObjectModel;
using System.IO.Compression;
using System.Text.Json;
using Windows.Storage;
using Windows.Storage.Pickers;
using WinRT.Interop;

namespace ProjectOtter.ViewModels;

public partial class MainViewModel : ObservableRecipient
{
    [ObservableProperty]
    private string fileName = "no .zip selected";

    [ObservableProperty]
    private string fileContent = string.Empty;

    [ObservableProperty]
    private ZipArchiveEntry? selectedEntry;

    public List<ZipArchiveEntry> AllZipArchiveEntries { get; set; } = new();

    public ObservableCollection<ZipArchiveEntry> DisplayZipEntries { get; set; } = new();

    [ObservableProperty]
    private bool hideEmptyFiles = true;

    [ObservableProperty]
    private string filterText = string.Empty;

    private readonly DispatcherTimer debounceTimer = new();

    public INavigationService NavigationService
    {
        get;
    }

    public MainViewModel(INavigationService navigationService)
    {
        debounceTimer.Interval = TimeSpan.FromMilliseconds(200);
        debounceTimer.Tick += DebounceTimer_Tick;

        NavigationService = navigationService;
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
        foreach (ZipArchiveEntry entry in AllZipArchiveEntries)
            DisplayZipEntries.Add(entry);
    }

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
        };

        foreach (string file in filesToRead)
        {
            ZipArchiveEntry? entry = AllZipArchiveEntries.FirstOrDefault(x => x.FullName.Equals(file, StringComparison.InvariantCultureIgnoreCase));

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
    }
}
