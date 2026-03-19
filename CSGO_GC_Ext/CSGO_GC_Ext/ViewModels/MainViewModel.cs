using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml.MarkupExtensions;
using Avalonia.Media;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CSGO_GC_Ext.Utils.Views;
using CSGO_GC_Ext.Views;
using System;
using System.ComponentModel;

namespace CSGO_GC_Ext.ViewModels;

/// <summary>
/// Remember to set the <see cref="MainViewModel.IsViewLoaded"/> property after View Loaded trigger.
/// </summary>
public partial class MainViewModel : ViewModelBase
{
    private static readonly IImmutableBrush __AppBackgroundBrush= new SolidColorBrush(Color.Parse("#232323")).ToImmutable();
    public static IImmutableBrush AppBackgroundBrush => __AppBackgroundBrush;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(AppBarStyle))]
    public partial Visual? CurrentView { get; set; } = null;

    public AppBarProperties DefaultAppBarStyle { get; set; }
    public new AppBarProperties AppBarStyle
    {
        get => (CurrentView?.DataContext as ViewModelBase)?.AppBarStyle ?? DefaultAppBarStyle;
    }

    private bool _isLoaded = false;
    [ObservableProperty]
    public partial bool IsLoaded { get; set; } = false;

    private readonly HomeView _viewHome;
    private readonly SettingsView _viewSettings;
    private readonly AboutView _viewAbout;

    public MainViewModel(Visual? testParent)
    {
        var title = new TextBlock();
        title.Bind(TextBlock.TextProperty, new DynamicResourceExtension("AppTitle"));
        var titleTag = new TextBlock();
        titleTag.Bind(TextBlock.TextProperty, new DynamicResourceExtension("AppDisplayVersion"));
        DefaultAppBarStyle = new()
        {
            TitleContent = title,
            TitleTagContent = titleTag,
        };

        if (testParent is not null)
        {
            _viewHome = new(testParent);
            _viewSettings = new(testParent);
            _viewAbout = new(testParent);
        }
        else
        {
            _viewHome = new();
            _viewSettings = new();
            _viewAbout = new();
        }
    }

    public MainViewModel() : this(null)
    {
    }

    protected override void OnPropertyChanged(PropertyChangedEventArgs e)
    {
        if(e.PropertyName is nameof(IsLoaded))
        {
            if (IsLoaded)
            {
                _isLoaded = true;

                if (!Design.IsDesignMode)
                    NavigateHome();
            }
            else if (_isLoaded)
                throw new InvalidOperationException();
        }

        base.OnPropertyChanged(e);
    } 

    private bool Navigate(Visual content)
    {
        var ina = content as ViewHelper.INavigationAnimatable;

        if (!ina?.OnNavigating() ?? false) return false;
        CurrentView = content;
        Dispatcher.UIThread.Invoke(content.InvalidateVisual);
        CurrentView = null; // For making correct style resource bindings, FUCK Avalonia.
        Dispatcher.UIThread.Invoke(content.InvalidateVisual);
        CurrentView = content;
        ina?.OnNavigated();

        return true;
    }

    [RelayCommand]
    public void NavigateHome()
    {
        Navigate(_viewHome);
    }

    [RelayCommand]
    public void NavigateSettings()
    {
        Navigate(_viewSettings);
    }

    [RelayCommand]
    public void NavigateAbout()
    {
        Navigate(_viewAbout);
    }
}

