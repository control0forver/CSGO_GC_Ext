using FuzzySharp;
using System.Collections.Generic;
using System.Linq;

namespace CSGO_GC_Ext.Utils;

public interface IInventoryItemSearcherSearchable
{
    IEnumerable<string> SearchTokens { get; }
}

//public class InventoryItemSearcher<T>(params T[] items)
//    where T : class, IInventoryItemSearcherSearchable
//{
//    public readonly List<T> Items = [.. items];
//
//    public IEnumerable<T> Search(string userRawInput, int threshold = 70)
//        => InventoryItemSearcher<T>.Search<T>(userRawInput, threshold, items);

public static class InventoryItemSearcher
{
    public static IEnumerable<T> Search<T>(string? userRawInput, int threshold = 70, params IEnumerable<T> items)
        where T : class, IInventoryItemSearcherSearchable
    {
        if (string.IsNullOrWhiteSpace(userRawInput))
            return items;

        return items
            .Select(item => new {
                Item = item,
                Score = item.SearchTokens
                    .Max(field => Fuzz.PartialRatio(userRawInput, field))
            })
            .Where(x => x.Score >= threshold)
            .OrderByDescending(x => x.Score)
            .Select(x => x.Item)
            .ToList();
    }
}
