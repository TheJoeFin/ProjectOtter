using ProjectOtter.Helpers;

namespace Tests;

public class UnitTest1
{
    [Fact]
    public void DateFromFileNameParsingTest()
    {
        string imageResize = @"Image Resizer/ModuleInterface/Logs/v0.79.0/log_2024-03-18.txt";
        string settingsJson = @"Image Resizer/settings.json";
        string logSettingsJson = @"log_settings.json";

        string findMyMouse = @"FindMyMouse/ModuleInterface/Logs/v0.79.0/log_2024-03-18.txt";
        string awakeLog = @"Awake/Logs/awake-log_2024-03-06.txt";
        string awakeVersionLog = @"Awake/Logs/0.77.0.0/Log_2024-03-13.txt";

        List<string> fileNames =
        [
            imageResize,
            settingsJson,
            logSettingsJson,
            findMyMouse,
            awakeLog,
            awakeVersionLog
        ];

        foreach (string fullPath in fileNames)
        {
            DateOnly date = ExtractDateFromFilePath(fullPath);
            Assert.NotNull(date);
        }
    }

    public static DateOnly ExtractDateFromFilePath(string filePath)
    {
        // Extract the date portion from the file path
        // string dateString = filePath.Substring(filePath.LastIndexOf('/') + 1, 10);
        string fileName = Path.GetFileName(filePath);

        // Parse the date string into a DateOnly object
        DateOnly date = DateOnly.Parse(dateString);

        return date;
    }
}