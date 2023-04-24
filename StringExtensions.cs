using System.Runtime.CompilerServices;

namespace protodec;

public static class StringExtensions
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int CountUpper(this string str, int i = 0)
    {
        int upper = 0;

        for (; i < str.Length; i++)
            if (char.IsAsciiLetterUpper(str[i]))
                upper++;

        return upper;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    // ReSharper disable once IdentifierTypo
    public static bool IsBeebyted(this string name) =>
        name.Length == 11 && CountUpper(name) == 11;

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
}