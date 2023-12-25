using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace LibProtodec;

public static class Extensions
{
    public static void Add<TKey, TValue>(this ICollection<KeyValuePair<TKey, TValue>> keyValuePairs, TKey key, TValue value) =>
        keyValuePairs.Add(new KeyValuePair<TKey, TValue>(key, value));

    public static bool ContainsDuplicateKey<TKey, TValue>(
        this IEnumerable<KeyValuePair<TKey, TValue>> keyValuePairs,
        IEqualityComparer<TKey>?                     comparer = null)
    {
        HashSet<TKey> set = new(5, comparer);

        foreach (KeyValuePair<TKey, TValue> kvp in keyValuePairs)
        {
            if (!set.Add(kvp.Key))
            {
                return true;
            }
        }

        return false;
    }

    public static string TrimEnd(this string @string, string trimStr) =>
        @string.EndsWith(trimStr, StringComparison.Ordinal)
            ? @string[..^trimStr.Length]
            : @string;

    public static string ToSnakeCaseLower(this string str) =>
        string.Create(str.Length + CountUpper(str, 1), str, (newString, oldString) =>
        {
            newString[0] = char.ToLowerInvariant(oldString[0]);

            char chr;
            for (int i = 1, j = 1; i < oldString.Length; i++, j++)
            {
                chr = oldString[i];

                if (char.IsAsciiLetterUpper(chr))
                {
                    newString[j++] = '_';
                    newString[j]   = char.ToLowerInvariant(chr);
                }
                else
                {
                    newString[j] = chr;
                }
            }
        });

    public static string ToSnakeCaseUpper(this string str) =>
        string.Create(str.Length + CountUpper(str, 1), str, (newString, oldString) =>
        {
            newString[0] = char.ToUpperInvariant(oldString[0]);

            char chr;
            for (int i = 1, j = 1; i < oldString.Length; i++, j++)
            {
                chr = oldString[i];

                if (char.IsAsciiLetterUpper(chr))
                {
                    newString[j++] = '_';
                    newString[j]   = chr;
                }
                else
                {
                    newString[j] = char.ToUpperInvariant(chr);
                }
            }
        });

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    // ReSharper disable once IdentifierTypo
    public static bool IsBeebyted(this string name) =>
        name.Length == 11 && CountUpper(name) == 11;
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int CountUpper(this string str, int i = 0)
    {
        int upper = 0;

        for (; i < str.Length; i++)
            if (char.IsAsciiLetterUpper(str[i]))
                upper++;

        return upper;
    }
}