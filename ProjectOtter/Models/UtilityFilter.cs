using Microsoft.UI.Xaml;

namespace ProjectOtter.Models;

public record UtilityFilter
{
    public string UtilityName { get; set; } = string.Empty;

    private bool isFiltering;

    public bool IsFiltering
    {
        get { return isFiltering; }
        set
        {
            if (isFiltering = value)
                return;

            isFiltering = value;
            FilteringChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    public event EventHandler? FilteringChanged;

}
