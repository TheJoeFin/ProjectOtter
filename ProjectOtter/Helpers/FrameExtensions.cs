using Microsoft.UI.Xaml.Controls;

namespace ProjectOtter.Helpers;

public static class FrameExtensions
{
    public static object? GetPageViewModel(this Frame frame)
    {
        var content = frame?.Content;
        var contentType = content?.GetType();
        var viewModelProperty = contentType?.GetProperty("ViewModel");
        var viewModel = viewModelProperty?.GetValue(content, null);

        return viewModel;
    }
}
