using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace CSGO_GC_Ext.Utils;

public static class Translations
{
    //public static IEnumerable<(string DisplayName, string SourceName)> GetTranslationEnumerations()
    //    => ((ResourceDictionary)AvaloniaXamlLoader.Load(GetTranslationSourceReference("_languages")))
    //        .Select(t =>
    //        (
    //            t.Key as string ?? __GetTranslationEnumerations_raise_invalid_string_resource(),
    //            t.Value as string ?? __GetTranslationEnumerations_raise_invalid_string_resource()
    //        ));
    //private static string __GetTranslationEnumerations_raise_invalid_string_resource() => throw new InvalidOperationException("Invalid translations file. (_translations.axaml)");

    //public static Uri GetTranslationSourceReference(string sourceName, string? internalAssembly = null)
    //{
    //    var uri = $"Assets/Translations/{sourceName}.axaml";
    //    if (internalAssembly is null)
    //    {
    //        var _1 = Environment.CurrentDirectory;
    //        var _2 = Path.Combine(_1, uri);
    //        return new Uri(new Uri("file://"), Path.GetFullPath(_2));
    //    }
    //    else
    //    {
    //        return new($"avares://{internalAssembly}/" + uri);
    //    }
    //}

    //public static (string DisplayName, string SourceName) GetDefaultTranslation()
    //{
    //    var _ts = GetTranslationEnumerations();

    //    return _ts.Single(t => t.DisplayName == _ts.Single(t => t.DisplayName == "_default").SourceName);
    //}


    ///// <summary>
    ///// Get a translation dictionary.
    ///// </summary>
    ///// <param name="sourceName">use default translation if null (GetDefaultTranslation)</param>
    ///// <returns>A dictionary contains translaitons.</returns>
    //public static ResourceDictionary GetTranslationResource(string? sourceName = null) 
    //    => (ResourceDictionary)AvaloniaXamlLoader.Load(GetTranslationSourceReference(sourceName ?? GetDefaultTranslation().SourceName));
}
