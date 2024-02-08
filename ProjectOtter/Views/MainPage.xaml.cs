using Microsoft.UI.Xaml.Controls;
using ProjectOtter.ViewModels;

namespace ProjectOtter.Views;

public sealed partial class MainPage : Page
{
    public MainViewModel ViewModel { get; }

    public MainPage()
    {
        ViewModel = App.GetService<MainViewModel>();
        InitializeComponent();
    }
}
