using Microsoft.UI.Xaml.Controls;
using ProjectOtter.ViewModels;

namespace ProjectOtter.Views;

public sealed partial class MainPage : Page
{
    public MainViewModel mvm;

    public MainPage()
    {
        mvm = App.GetService<MainViewModel>();
        InitializeComponent();
    }
}
