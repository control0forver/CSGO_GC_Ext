using Avalonia;
using Avalonia.Animation;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Threading;
using Avalonia.VisualTree;
using CSGO_GC_Ext.Utils;
using CSGO_GC_Ext.ViewModels;
using log4net;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CSGO_GC_Ext.Views;

public partial class MainView : UserControl
{
    private static readonly ILog Log = LogManager.GetLogger(typeof(MainView));
    private new MainViewModel DataContext
    {
        get => (MainViewModel)(base.DataContext ?? throw new("DataContext is null."));
        set => base.DataContext = value;
    }

    public MainView()
    {
        InitializeComponent();

        if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime)
        {
            // Not available
#if DEBUG
            MenuButtonExit.IsEnabled = false;
            ButtonExit.IsEnabled = false;
            ButtonToggleWindowState.IsEnabled = false;
            ButtonMinimizeWindow.IsEnabled = false;
#else
            MenuButtonExit.IsVisible = false;
            ButtonExit.IsVisible = false;
            ButtonToggleWindowState.IsVisible = false;
            ButtonMinimizeWindow.IsVisible = false;
#endif
        }

        this.Loaded += OnLoaded; ;

        AppBar.PointerPressed += OnAppBarPointerPressed;
        AppBar.DoubleTapped += OnAppBarDoubleTapped;
    }

    private void OnLoaded(object? sender, RoutedEventArgs e)
    {
        DataContext = new(this);

        void __LayoutUpdated(object? sender, EventArgs e)
        {
            this.LayoutUpdated -= __LayoutUpdated;
            DataContext.IsLoaded = true;
        }
        this.LayoutUpdated += __LayoutUpdated;
    }

    private void OnExitClick(object? sender, RoutedEventArgs e)
    {
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktopApp)
        {
            desktopApp.Shutdown();
        }
        else
        {
            throw new NotImplementedException();
        }
    }

    private void OnToggleWindowStateClick(object? sender, RoutedEventArgs e)
    {
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktopApp)
        {
            if (desktopApp.MainWindow is null)
                throw new("MainWindow is null");

            if (desktopApp.MainWindow.WindowState == WindowState.FullScreen)
            {
                desktopApp.MainWindow.WindowState = WindowState.Normal;
            }
            else if (desktopApp.MainWindow.WindowState == WindowState.Maximized)
            {
                desktopApp.MainWindow.WindowState = WindowState.FullScreen;
            }
            else if (desktopApp.MainWindow.WindowState == WindowState.Normal)
            {
                desktopApp.MainWindow.WindowState = WindowState.Maximized;
            }
            else
                desktopApp.MainWindow.WindowState = WindowState.Normal;
        }
        else
        {
            throw new NotImplementedException();
        }
    }

    private void OnMinimizeWindowClick(object? sender, RoutedEventArgs e)
    {
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktopApp)
        {
            if (desktopApp.MainWindow is null)
                throw new("MainWindow is null");

            desktopApp.MainWindow.WindowState = WindowState.Minimized;
        }
        else
        {
            throw new NotImplementedException();
        }
    }

    //private void OnTestButtonClick(object? sender, RoutedEventArgs e)
    //{
    //    DataContext.MaxItemsPerPage = 10; // 40
    //    _ = DataContext.DispatchUpdate();
    //}

    private void OnAppBarPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (this.Parent is not Drivers.MainWindow mainWindow)
            return;

        mainWindow.MoveWindow = true;
    }

    private void OnAppBarDoubleTapped(object? sender, TappedEventArgs e)
    {
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktopApp)
        {
            if (desktopApp.MainWindow is null)
                throw new("MainWindow is null");

            if (desktopApp.MainWindow.WindowState == WindowState.FullScreen)
            {
                desktopApp.MainWindow.WindowState = WindowState.Normal;
            }
            else if (desktopApp.MainWindow.WindowState == WindowState.Maximized)
            {
                desktopApp.MainWindow.WindowState = WindowState.FullScreen;
            }
            else if (desktopApp.MainWindow.WindowState == WindowState.Normal)
            {
                desktopApp.MainWindow.WindowState = WindowState.Maximized;
            }
            else
                desktopApp.MainWindow.WindowState = WindowState.Normal;
        }
    }
}