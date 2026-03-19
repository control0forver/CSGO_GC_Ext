using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace CSGO_GC_Ext.Utils.Game;

public static class DictionaryExtensions
{
    public static Dictionary<TKey, object>? TryGetSelfRecursiveValue<TKey>(this Dictionary<TKey, object> @this, TKey key)
        where TKey : notnull
    {
        if (@this.TryGetValue(key, out var _v) && _v is Dictionary<TKey, object> _vd)
            return _vd;
        else
            return null;
    }

    public static string? TryGetStringValue<TKey>(this Dictionary<TKey, object> @this, TKey key, bool stricted = false)
        where TKey : notnull
    {
        if (@this.TryGetValue(key, out var _v))
        {
            if (_v is string _vs)
                return _vs;
            else if (!stricted)
            {
                var _vcs = @this.TryGetCSGOTxtStringValueValue(key);
                if (_vcs != null)
                    return _vcs.FirstOrDefault()?.Item1 ?? null;
            }
        }

        return null;
    }

    public static IEnumerable<Tuple<string, IEnumerable<string>?>>? TryGetCSGOTxtStringValueValue<TKey>(this Dictionary<TKey, object> @this, TKey key)
        where TKey : notnull
    {
        if (@this.TryGetValue(key, out var _v) && _v is IEnumerable<Tuple<string, IEnumerable<string>?>> _vx)
            return _vx;
        else
            return null;
    }
}

//public static class StringUnionExtensions
//{
//    public static CSGOTxtHelper.StringUnion ToStringUnion(this string value) => new(value);
//
//    public static CSGOTxtHelper.StringUnion ToStringUnion(this IEnumerable<KeyValuePair<string, string>> pairs) => new(pairs);
//
//    public static bool TryGetValue(this CSGOTxtHelper.StringUnion union, string key, out string value)
//    {
//        value = string.Empty;
//
//        if (!union.IsPairs)
//            return false;
//
//        foreach (var pair in union.Pairs)
//        {
//            if (pair.Key == key)
//            {
//                value = pair.Value;
//                return true;
//            }
//        }
//
//        return false;
//    }
//
//    public static CSGOTxtHelper.StringUnion AddPair(this CSGOTxtHelper.StringUnion union, string key, string value)
//    {
//        if (union.IsSingle)
//        {
//            throw new InvalidOperationException("Cannot add pair to single string union");
//        }
//
//        var pairs = union.IsPairs
//            ? union.Pairs.ToList()
//            : [];
//
//        pairs.Add(new KeyValuePair<string, string>(key, value));
//        return new CSGOTxtHelper.StringUnion(pairs);
//    }
//}

public static class CSGOTxtHelper
{
    // TODO: CSGOJsonLikeTxtHelper&CSGOItemsGameCdnTxtHelper: Use ObservableObject, instead of static class.
    public static class CSGOJsonLikeTxtHelper
    {
        public enum ValueType
        {
            String,
            IntString,
            FloatString,
            Scope,
        }

        //[StructLayout(LayoutKind.Explicit)]
        //public readonly struct StringUnion : IEnumerable<string>, IEquatable<StringUnion>
        //{
        //    [FieldOffset(0)]
        //    private readonly string? _single;
        //
        //    [FieldOffset(0)]
        //    private readonly StringPairCollection? _pairs;
        //
        //    [FieldOffset(8)]
        //    private readonly UnionType _type;
        //
        //    public StringUnion(string value)
        //    {
        //        _single = value ?? throw new ArgumentNullException(nameof(value));
        //        _pairs = null;
        //        _type = UnionType.Single;
        //    }
        //
        //    public StringUnion(IEnumerable<KeyValuePair<string, string>> pairs)
        //    {
        //        ArgumentNullException.ThrowIfNull(pairs);
        //
        //        _single = null;
        //        _pairs = new StringPairCollection(pairs);
        //        _type = UnionType.Pairs;
        //    }
        //
        //    public bool IsSingle => _type == UnionType.Single;
        //    public bool IsPairs => _type == UnionType.Pairs;
        //
        //    public string Single => IsSingle ? _single! : throw new InvalidOperationException("Not a single string.");
        //
        //    public IReadOnlyList<KeyValuePair<string, string>> Pairs =>
        //        IsPairs ? _pairs!.AsReadOnly() : throw new InvalidOperationException("Not a pairs collection.");
        //
        //    public override string ToString()
        //    {
        //        return _type switch
        //        {
        //            UnionType.Single => _single ?? string.Empty,
        //            UnionType.Pairs => _pairs?.ToString() ?? string.Empty,
        //            _ => string.Empty
        //        };
        //    }
        //
        //    public IEnumerator<string> GetEnumerator()
        //    {
        //        if (IsSingle)
        //        {
        //            yield return _single!;
        //        }
        //        else if (IsPairs && _pairs is not null)
        //        {
        //            foreach (var pair in _pairs)
        //            {
        //                yield return pair.Key;
        //                yield return pair.Value;
        //            }
        //        }
        //    }
        //
        //    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        //
        //    public bool Equals(StringUnion other)
        //    {
        //        if (_type != other._type)
        //            return false;
        //
        //        return _type switch
        //        {
        //            UnionType.Single => string.Equals(_single, other._single),
        //            UnionType.Pairs => _pairs?.Equals(other._pairs) ?? false,
        //            _ => true
        //        };
        //    }
        //
        //    public override bool Equals(object? obj) => obj is StringUnion other && Equals(other);
        //
        //    public override int GetHashCode()
        //    {
        //        return _type switch
        //        {
        //            UnionType.Single => _single?.GetHashCode() ?? 0,
        //            UnionType.Pairs => _pairs?.GetHashCode() ?? 0,
        //            _ => 0
        //        };
        //    }
        //
        //    public static implicit operator StringUnion(string value) => new(value);
        //    public static implicit operator string(StringUnion union) => union.ToString();
        //
        //    public static bool operator ==(StringUnion left, StringUnion right) => left.Equals(right);
        //    public static bool operator !=(StringUnion left, StringUnion right) => !left.Equals(right);
        //
        //    private enum UnionType : byte
        //    {
        //        None,
        //        Single,
        //        Pairs
        //    }
        //
        //    // 专门优化的字符串对集合
        //    [StructLayout(LayoutKind.Sequential)]
        //    private sealed class StringPairCollection(IEnumerable<KeyValuePair<string, string>> pairs) : IEquatable<StringPairCollection>, IEnumerable<KeyValuePair<string, string>>
        //    {
        //        private readonly KeyValuePair<string, string>[] _pairs = [.. pairs];
        //
        //        public IReadOnlyList<KeyValuePair<string, string>> AsReadOnly() => _pairs;
        //
        //        public IEnumerator<KeyValuePair<string, string>> GetEnumerator() =>
        //            ((IEnumerable<KeyValuePair<string, string>>)_pairs).GetEnumerator();
        //
        //        IEnumerator IEnumerable.GetEnumerator() => _pairs.GetEnumerator();
        //
        //        public bool Equals(StringPairCollection? other)
        //        {
        //            if (other is null) return false;
        //            if (ReferenceEquals(this, other)) return true;
        //
        //            return _pairs.AsSpan().SequenceEqual(other._pairs.AsSpan());
        //        }
        //
        //        public override bool Equals(object? obj) =>
        //            obj is StringPairCollection other && Equals(other);
        //
        //        public override int GetHashCode()
        //        {
        //            var hash = new HashCode();
        //            foreach (ref readonly var pair in _pairs.AsSpan())
        //            {
        //                hash.Add(pair.Key);
        //                hash.Add(pair.Value);
        //            }
        //            return hash.ToHashCode();
        //        }
        //
        //        public override string ToString() =>
        //            string.Join(", ", _pairs.Select(p => $"{p.Key}:{p.Value}"));
        //    }
        //}

        [AttributeUsage(AttributeTargets.All, AllowMultiple = false)]
        public class TxtPropertyAttribute : Attribute
        {
            public string? Key { get; set; }
            public bool Required { get; set; }
            public ValueType ValueType { get; set; }

            public IEnumerable<string>? ScopePropertiesKeys { get; set; }

            public TxtPropertyAttribute(string? key, ValueType valueType = default, bool required = true, params string[] scopeKeys)
            {
                Key = key;
                Required = required;
                ValueType = valueType;

                //if (valueType is ValueType.Scope && scopeKeys.Length < 0)
                //    throw new ArgumentNullException(nameof(scopeKeys), $"{nameof(scopeKeys)} must be provided when {nameof(valueType)} is {nameof(valueType.Scope)}");
                if (valueType is ValueType.Scope && scopeKeys.Length > 1)
                {
                    ScopePropertiesKeys = scopeKeys;
                }
                else
                {
                    ScopePropertiesKeys = null;
                }
            }
        }

        [ThreadStatic]
        private static int __recursive_calling_index;
        /// <summary>
        /// 
        /// </summary>
        /// <param name="reader"></param>
        /// <returns>A Dictionary; Available value types: self, string, IEnumerable<Tuple<string, IEnumerable<string>?>>.</returns>
        /// <remarks>Notice: Never call self recursively in different threads</remarks>
        public static Dictionary<string, object> Resolve(StreamReader reader)
        {
            ArgumentNullException.ThrowIfNull(reader);

            Dictionary<string, object> __func_call_self_recursively()
            {
                __recursive_calling_index++;
                var _v = Resolve(reader);
                __recursive_calling_index--;
                return _v;
            }

            var _result = new Dictionary<string, object>();

            int _char;
            while ((_char = reader.Read()) != -1)
            {
                #region Auxiliary Functions

                static bool __func_is_whitespace(char c)
                    => char.IsWhiteSpace(c);
                /// Notice: existingValue will not be modified if a IEnumerable<Tuple<string, IEnumerable<string>?>> type is passed in.
                /// Notice: if one of a IEnumerable<Tuple<string, IEnumerable<string>?>> type is passed in, a IEnumerable<Tuple<string, IEnumerable<string>?>> will be returned.
                static T __func_merge_values<T>(object existingValue, object newValue)
                {
                    // Both Scopes (Merge)
                    {
                        if (existingValue is Dictionary<string, object> existingDict &&
                            newValue is Dictionary<string, object> newDict)
                        {
                            foreach (var item in newDict)
                            {
                                ref var valueRef = ref CollectionsMarshal.GetValueRefOrAddDefault(existingDict, item.Key, out bool exists);
                                if (exists)
                                {
                                    valueRef = __func_obj_merge_values(valueRef!, item.Value);
                                }
                                else
                                {
                                    valueRef = item.Value;
                                }
                            }
                            return (T)(object)existingDict;
                        }
                    }
                    // Left single string, right Multi-platformed strings (Merge as a Multi-platformed string)
                    {
                        if (existingValue is string existingStr && newValue is IEnumerable<Tuple<string, IEnumerable<string>?>> newMultiStrs)
                        {
                            return (T)(object)new Collection<Tuple<string, IEnumerable<string>?>>([new(existingStr, null), .. newMultiStrs]);
                        }
                    }
                    // Left Multi-platformed strings, right single string (Merge as a Multi-platformed string)
                    {
                        if (existingValue is IEnumerable<Tuple<string, IEnumerable<string>?>> existingMultiStrs && newValue is string newStr)
                        {
                            return (T)(object)new Collection<Tuple<string, IEnumerable<string>?>>([.. existingMultiStrs, new(newStr, null)]);
                        }
                    }
                    // Both Multi-platformed strings (Merge)
                    {
                        if (existingValue is IEnumerable<Tuple<string, IEnumerable<string>?>> existingMultiStrs && newValue is IEnumerable<Tuple<string, IEnumerable<string>?>> newMultiStrs)
                        {
                            return (T)(object)new Collection<Tuple<string, IEnumerable<string>?>>([.. existingMultiStrs, .. newMultiStrs]);
                        }
                    }

                    // Overwrite existing value (Both-String, etc.)
                    return (T)newValue;
                }
                static object __func_obj_merge_values(object existingValue, object newValue) => __func_merge_values<object>(existingValue, newValue);
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                static void __func_skip_white_spaces_and_comments(StreamReader reader)
                {
                    int _char;
                    while ((_char = reader.Peek()) != -1)
                    {
                        if (__func_is_whitespace((char)_char))
                        {
                            reader.Read();
                            continue;
                        }

                        if ((char)_char == '/')
                        {
                            reader.Read();
                            if (reader.Peek() == '/')
                            {
                                reader.ReadLine();
                                continue;
                            }
                            else
                            {
                                reader.BaseStream.Position--;
                                break;
                            }
                        }

                        break;
                    }
                }
                /// Notice: Make sure to read out a '"' character from the stream before calling this function
                static (string value, IEnumerable<string>? platforms) __func_read_string_value(StreamReader reader, bool isReadingValue, char end = '"')
                {
                    StringBuilder sb = new();
                    int currentChar;
                    while ((currentChar = reader.Read()) != -1)
                    {
                        if (currentChar == end)
                            break;

                        // Escape Processing
                        if (currentChar == '\\')
                        {
                            int nextChar = reader.Read();
                            if (nextChar != -1)
                            {
                                sb.Append((char)nextChar);
                                continue;
                            }
                        }

                        sb.Append((char)currentChar);
                    }
                    (string value, IEnumerable<string>? platforms) _result = new()
                    {
                        value = sb.ToString()
                    };

                    __func_skip_white_spaces_and_comments(reader);

                    // Valve preprocessor directives (e.g., [$WIN32||$X360], [!$X360]); these commonly appear in CSGO translation files.
                    if (isReadingValue) // Always generated after values.
                        if (reader.Peek() != -1 && reader.Peek() == '[')
                        {
                            reader.Read();
                            sb.Clear();
                            _result.platforms = __func_read_vpp_directives(reader, sb: sb);
                        }

                    return _result;

                    /// Notice: Make sure to read out a '[' character from the stream before calling this function
                    static IEnumerable<string>? __func_read_vpp_directives(StreamReader reader, char end = ']', StringBuilder? sb = null)
                    {
                        const string __directive_separator = "||";

                        sb ??= new();
                        IEnumerable<string>? _directives = null;

                        int currentChar;
                        while ((currentChar = reader.Read()) != -1)
                        {
                            if (currentChar == end)
                                break;

                            // // Separator Check
                            // if (__directive_separator.Length > 0)
                            // {
                            //     bool __separator_found = false;
                            //     StringBuilder? _tmp_sb = null;
                            //     for (int i = 0; i <= __directive_separator.Length; i++)
                            //     {
                            //         if (i == __directive_separator.Length)
                            //         {
                            //             // Separator Found
                            //             __separator_found = true;
                            //             break;
                            //         }
                            //
                            //         if (currentChar != __directive_separator[i] ||
                            //             reader.Peek() == -1)
                            //         {
                            //             // Not match
                            //             if (_tmp_sb?.Length > 0)
                            //                 sb.Append(_tmp_sb); // Restore the read characters
                            //
                            //             break;
                            //         }
                            //
                            //         _tmp_sb ??= new();
                            //         _tmp_sb.Append((char)currentChar);
                            //         currentChar = reader.Read();
                            //         continue;
                            //     }
                            //
                            //     if (__separator_found)
                            //     {
                            //         // Separator Found
                            //         if (sb.Length > 0)
                            //             _directives.Add(sb.ToString());
                            //         sb.Clear();
                            //     }
                            //
                            //     continue;
                            // }
                            //
                            // // No seprator found

                            sb.Append((char)currentChar);
                            continue;
                        }

                        if (sb.Length > 0)
                        {
                            _directives = sb.ToString().Split(__directive_separator).Select(x => x.Trim());
                            sb.Clear();
                        }

                        return _directives;
                    }
                }

                #endregion

                // Skip whitespaces
                if (__func_is_whitespace((char)_char))
                    continue;

                // Key (String)
                if (_char == '"')
                {
                    var (value, platforms) = __func_read_string_value(reader, isReadingValue: false);
                    if (platforms != null)
                        throw new($"We may made some mistakes in the resolving or the file is not valid.\nMore info: Current string read: {value} [{string.Join(", ", platforms)}]");

                    string key = value;

                    __func_skip_white_spaces_and_comments(reader);
                    int nextChar = reader.Peek();

                    // Value (String)
                    if (nextChar == '"')
                    {
                        reader.Read();
                        var _value_str_read = __func_read_string_value(reader, isReadingValue: true);

                        ref var valueRef = ref CollectionsMarshal.GetValueRefOrAddDefault(_result, key, out bool exists);
                        if (exists)
                        {
                            valueRef = __func_obj_merge_values(valueRef!, _value_str_read);
                        }
                        else
                        {
                            // Pass string if not multi-platform string.
                            valueRef = _value_str_read.platforms is null ? _value_str_read.value : _value_str_read;
                        }
                    }
                    // Value (Scoping)
                    else if (nextChar == '{')
                    {
                        reader.Read();
                        Dictionary<string, object> nestedDict = __func_call_self_recursively();

                        ref var valueRef = ref CollectionsMarshal.GetValueRefOrAddDefault(_result, key, out bool exists);
                        if (exists)
                        {
                            valueRef = __func_obj_merge_values(valueRef!, nestedDict);
                        }
                        else
                        {
                            valueRef = nestedDict;
                        }
                    }
                }
                // End Scoping
                else if (_char == '}')
                {
                    break; // Return from this scope
                }
                // Value (Anonymous Scope - Keyless Pair)
                else if (_char == '{')
                {
                    Dictionary<string, object> nestedDict = __func_call_self_recursively();
                    // Merge to current scope
                    _result = __func_merge_values<Dictionary<string, object>>(_result, nestedDict);
                }
            }

            return _result;
        }

        public static Dictionary<string, object> Resolve(Stream stream)
        {
            ArgumentNullException.ThrowIfNull(stream);

            if (!stream.CanRead)
                throw new ArgumentException("Stream must be readable", nameof(stream));

            using var reader = new StreamReader(stream);
            return Resolve(reader);
        }

        public static Dictionary<string, object> Resolve(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
                throw new ArgumentException("File path cannot be null or empty", nameof(filePath));

            using var stream = File.OpenRead(filePath);
            return Resolve(stream);
        }


        public static void Save(Dictionary<string, object> data, StreamWriter writer)
        {
            ArgumentNullException.ThrowIfNull(writer);
            __func_save_dictionary(data, writer, 0);

            static void __func_save_dictionary(Dictionary<string, object> dict, StreamWriter writer, int indentLevel)
            {
                string indent = new(' ', indentLevel * 4);

                foreach (var kvp in dict)
                {
                    if (kvp.Value is Dictionary<string, object> nestedDict)
                    {
                        writer.WriteLine($"{indent}\"{kvp.Key}\"");
                        writer.WriteLine($"{indent}{{");
                        __func_save_dictionary(nestedDict, writer, indentLevel + 1);
                        writer.WriteLine($"{indent}}}");
                    }
                    else if (kvp.Value is IEnumerable<Tuple<string, IEnumerable<string>?>> multiPlatformStrings)
                    {
                        foreach (var (value, platforms) in multiPlatformStrings)
                        {
                            string platformSuffix = platforms != null ? $" [{string.Join("||", platforms)}]" : "";
                            writer.WriteLine($"{indent}\"{kvp.Key}\" \"{value}\"{platformSuffix}");
                        }
                    }
                    else if (kvp.Value is string stringValue)
                    {
                        writer.WriteLine($"{indent}\"{kvp.Key}\" \"{stringValue}\"");
                    }
                    else
                    {
                        writer.WriteLine($"{indent}\"{kvp.Key}\" \"{kvp.Value}\"");
                    }
                }
            }
        }

        public static void Save(Dictionary<string, object> data, Stream stream)
        {
            ArgumentNullException.ThrowIfNull(stream);

            if (!stream.CanWrite)
                throw new ArgumentException("Stream must be writable", nameof(stream));

            using var writer = new StreamWriter(stream);
            Save(data, writer);
        }

        public static void Save(Dictionary<string, object> data, string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
                throw new ArgumentException("File path cannot be null or empty", nameof(filePath));

            using var stream = File.Create(filePath);
            Save(data, stream);
        }
    }

    public static class CSGOItemsGameCdnTxtHelper
    {
        public static Dictionary<string, string> Resolve(Stream stream)
        {
            ArgumentNullException.ThrowIfNull(stream);

            if (!stream.CanRead)
                throw new ArgumentException("Stream must be readable", nameof(stream));

            using var reader = new StreamReader(stream);
            return Resolve(reader);
        }

        public static Dictionary<string, string> Resolve(StreamReader reader)
        {
            ArgumentNullException.ThrowIfNull(reader);

            var lines = new List<string>();

            string? line;
            while ((line = reader.ReadLine()) != null)
            {
                lines.Add(line);
            }

            return ProcessLines(lines);
        }

        public static Dictionary<string, string> Resolve(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
                throw new ArgumentException("File path cannot be null or empty", nameof(filePath));

            using var stream = File.OpenRead(filePath);
            return Resolve(stream);
        }

        public static Dictionary<string, string> Resolve(params IEnumerable<string> lines)
        {
            ArgumentNullException.ThrowIfNull(lines);

            return ProcessLines(lines);
        }

        private static Dictionary<string, string> ProcessLines(IEnumerable<string> lines)
        {
            var validLines = lines
                .Where(line => !string.IsNullOrWhiteSpace(line))
                .Select(line => line.Trim())
                .Where(line => !line.StartsWith('#'));

            var result = new Dictionary<string, string>();

            foreach (var line in validLines)
            {
                var splitIndex = line.IndexOf('=');

                if (splitIndex <= 0)
                    goto parse_fail;

                var key = line[..splitIndex].Trim();
                var value = line[(splitIndex + 1)..].Trim();

                if (string.IsNullOrEmpty(key))
                    goto parse_fail;

                result[key] = value;
                continue;

            parse_fail:
                result.Add("#PARSE_FAIL", line);
                continue;
            }

            return result;
        }
    }
}
