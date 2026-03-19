using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core.Plugins;
using Avalonia.Markup.Xaml;
using Avalonia.Styling;
using log4net;
using log4net.Appender;
using log4net.Core;
using log4net.Layout;
using log4net.Repository.Hierarchy;
using System.Linq;

namespace CSGO_GC_Ext;

public partial class App : Application
{
    //private readonly MergeResourceInclude _languageResource = new(baseUri:null)
    //{
    //    Source = Utils.Translations.GetTranslationSourceReference("__base", "CSGO_GC_Ext")
    //};

    public App() { }

    private static void InitializeLog4NetToDebugOutput()
    {
        var hierarchy = (Hierarchy)LogManager.GetRepository();
        hierarchy.Root.RemoveAllAppenders();

        var patternLayout = new PatternLayout
        {
            ConversionPattern = "%date [%thread] %-5level %logger - %message%newline"
        };
        patternLayout.ActivateOptions();
        var debugAppender = new DebugAppender
        {
            Layout = patternLayout,
            Name = "DebugAppender"
        };
        debugAppender.ActivateOptions();

        hierarchy.Root.AddAppender(debugAppender);
        hierarchy.Root.Level = Level.All;
        hierarchy.Configured = true;
    }

    public override void Initialize()
    {
        InitializeLog4NetToDebugOutput();

        AvaloniaXamlLoader.Load(this);

        //this.Resources.MergedDictionaries.Add(this._languageResource);
        //LoadLanguage(); // Load default language.
    }

    //public void LoadLanguage(string? sourceName = null)
    //{
    //    var _new_lang = Utils.Translations.GetTranslationResource(sourceName);
    //    
    //    foreach (var token in this._languageResource.Loaded.Keys)
    //    {
    //        if (!_new_lang.TryGetValue(token, out var newTranslation))
    //        {
    //            // TODO: translaiton not found.
    //            continue;
    //        }
    //
    //        if (newTranslation is not string newTranslationString)
    //            throw new($"Invalid translation data: {sourceName}['{token}'], a string is excepted, but {newTranslation?.GetType().ToString() ?? "null object"} got.");
    //
    //        this._languageResource.Loaded[token] = newTranslationString;
    //    }
    //}


    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            /// Avoid duplicate validations from both Avalonia and the CommunityToolkit. 
            /// More info: https://docs.avaloniaui.net/docs/guides/development-guides/data-validation#manage-validationplugins
            static void __DisableAvaloniaDataAnnotationValidation()
            {
                // Get an array of plugins to remove
                var dataValidationPluginsToRemove =
                    BindingPlugins.DataValidators.OfType<DataAnnotationsValidationPlugin>().ToArray();

                // remove each entry found
                foreach (var plugin in dataValidationPluginsToRemove)
                {
                    BindingPlugins.DataValidators.Remove(plugin);
                }
            }
            __DisableAvaloniaDataAnnotationValidation();

            desktop.MainWindow = new Views.Drivers.MainWindow()
            {
                Content = new Views.MainView()
            };
        }
        else if (ApplicationLifetime is ISingleViewApplicationLifetime singleViewPlatform)
        {
            singleViewPlatform.MainView = new Views.MainView();
        }

        base.OnFrameworkInitializationCompleted();
    }
}