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
        ContentsListView.ItemClick += ContentsListView_ItemClick;
    }

    private void ContentsListView_ItemClick(object sender, ItemClickEventArgs e)
    {
        if (e.ClickedItem is ZipEntryItem clickedItem)
        {
            ViewModel.SelectCompareFileCommand.Execute(clickedItem);
        }
    }
}
