namespace ProjectOtter.Helpers;
internal static class AppWindowHelpers
{
    public static IntPtr GetHandle(this Microsoft.UI.Xaml.Window window)
    {
        return WinRT.Interop.WindowNative.GetWindowHandle(window);
    }
}
