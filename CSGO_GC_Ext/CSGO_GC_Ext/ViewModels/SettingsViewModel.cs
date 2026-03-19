using Avalonia.Controls;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CSGO_GC_Ext.Models;
using CSGO_GC_Ext.Utils;
using CSGO_GC_Ext.Utils.Game;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CSGO_GC_Ext.ViewModels;

public partial class SettingsViewModel : ViewModelBase
{
    [ObservableProperty]
    public partial IImmutableBrush Background { get; set; } = Design.IsDesignMode ? MainViewModel.AppBackgroundBrush : Brushes.Transparent;

    public SettingsViewModel()
    {
        this.AppBarStyle ??= new();
        this.AppBarStyle.TitleContent = "Settings";
        this.AppBarStyle.TitleTagContent= "";

    }
}

