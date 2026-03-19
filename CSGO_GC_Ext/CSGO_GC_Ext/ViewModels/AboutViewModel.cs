using Avalonia.Controls;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using System.ComponentModel;

namespace CSGO_GC_Ext.ViewModels;

public partial class AboutViewModel : ViewModelBase
{
    [ObservableProperty]
    public partial IImmutableBrush Background { get; set; } = Design.IsDesignMode ? MainViewModel.AppBackgroundBrush : Brushes.Transparent;

    public static string AppTitle => (App.Current?.Resources?.TryGetResource("AppTitle", null, out var v) is bool _b && _b && v is string str) ? str : string.Empty;
    public static string AppDisplayVersion => (App.Current?.Resources?.TryGetResource("AppDisplayVersion", null, out var v) is bool _b && _b && v is string str) ? str : string.Empty;

    public AboutViewModel()
    {
        this.AppBarStyle ??= new();
        this.AppBarStyle.TitleContent = "About";
        this.AppBarStyle.TitleTagContent= "";
    }
}

