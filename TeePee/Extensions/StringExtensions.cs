using System;

namespace TeePee.Extensions
{
    internal static class StringExtensions
    {
        internal static bool IsSameString(this string source, string dest) => string.Equals(source, dest, StringComparison.OrdinalIgnoreCase);

        internal static string Trunc(this string value) => value.Length > 50 ? $"{value.Substring(0, 50)}..." : value;
    }
}