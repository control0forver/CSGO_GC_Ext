using Avalonia;
using Avalonia.Controls;
using CSGO_GC_Ext.Models;
using CSGO_GC_Ext.Utils.Views;
using CSGO_GC_Ext.ViewModels;
using log4net;
using System.Threading.Tasks;

namespace CSGO_GC_Ext.Views;

public partial class EditItemView : UserControl, ViewHelper.INavigationAnimatable
{
    private static readonly ILog Log = LogManager.GetLogger(typeof(SettingsView));
    private new EditItemViewModel DataContext
    {
        get => (EditItemViewModel)(base.DataContext ?? throw new("DataContext is null."));
        set => base.DataContext = value;
    }

    private readonly ViewHelper.SlideAnimation _pageSlideInAnimation = new();

    // private EditItemView(Visual testParent)
    // {
    //     this.Arrange(testParent.Bounds);
    // }
    public EditItemView(EditItemViewModel viewData)
    {
        if (!Design.IsDesignMode)
        {
            DataContext = viewData;
        }

        InitializeComponent();
    }

    private void LoadSlideInAnimation()
    {
        _pageSlideInAnimation.Clear();
        _pageSlideInAnimation.Load(this.FindVisualChildren<Visual>(-1, [typeof(ContentControl)]));
    }

    public bool OnNavigating(object? _ = null)
    {
        LoadSlideInAnimation();

        return true;
    }

    public void OnNavigated(object? _ = null)
    {
        _pageSlideInAnimation.Fire(ViewHelper.SlideAnimation.ActType.In, reset: true);
    }
}