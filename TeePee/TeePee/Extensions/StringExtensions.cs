using System;

namespace TeePee.Extensions
{
    internal static class StringExtensions
    {
        internal static bool IsSameString(this string? source, string? dest, bool caseSensitive) => string.Equals(source, dest, caseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase);
        internal static bool IsSameUrl(this string? source, string? dest) => IsSameString(source, dest, false);

        internal static string Trunc(this string value, int? truncateBodyLength)
        {
            if (truncateBodyLength == null)
                return value;

            return value.Length > truncateBodyLength.Value ? $"{value.Substring(0, truncateBodyLength.Value)}..." : value;
        }
    }
}