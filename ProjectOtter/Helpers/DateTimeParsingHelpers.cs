using System;

namespace ProjectOtter.Helpers;

public static class DateTimeParsingHelpers
{
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
