using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Xaml.Controls;
using Nebula.Helpers.Enumeration;
using System.ComponentModel;


namespace Nebula.Features.ViewHost;

[INotifyPropertyChanged]
public sealed partial class MainWindowCloseDialog : ContentDialog
{


    public MainWindowCloseDialog()
    {
        this.InitializeComponent();
    }



    public EnumItemsSource<MainWindowCloseOption> MainWindowCloseOption
    {
        get;
        set => SetProperty(ref field, value);
    } = new(ViewHost.MainWindowCloseOption.Hide);



}
