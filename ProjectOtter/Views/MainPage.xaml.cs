using Microsoft.UI.Xaml.Controls;
using ProjectOtter.Models;
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

    private void CompareInfoBar_Closing(InfoBar sender, InfoBarClosingEventArgs args)
    {
        // Exit compare mode when InfoBar is closed
        ViewModel.ExitCompareCommand.Execute(null);
    }
}
