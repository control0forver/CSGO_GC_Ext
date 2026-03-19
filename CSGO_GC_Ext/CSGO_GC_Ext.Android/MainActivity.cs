using Android.App;
using Android.Content.PM;
using Avalonia;
using Avalonia.Android;
using Projektanker.Icons.Avalonia;
using Projektanker.Icons.Avalonia.FontAwesome;

namespace CSGO_GC_Ext.Android;

[Activity(
    Label = "CSGO_GC_Ext.Android",
    Theme = "@style/MyTheme.NoActionBar",
    Icon = "@drawable/icon",
    MainLauncher = true,
    ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.ScreenSize | ConfigChanges.UiMode)]
public class MainActivity : AvaloniaMainActivity<App>
{
    protected override AppBuilder CustomizeAppBuilder(AppBuilder builder)
    {
        IconProvider.Current.Register<FontAwesomeIconProvider>();

        return base.CustomizeAppBuilder(builder)
            .WithInterFont();
    }
}
