using System;

namespace TeePee.Extensions
{
    internal static class StringExtensions
    {
        private const int _TRUNCATE_BODY_LENGTH = 500;

        internal static bool IsSameString(this string? source, string? dest, bool caseSensitive) => string.Equals(source, dest, caseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase);
        internal static bool IsSameUrl(this string? source, string? dest) => IsSameString(source, dest, false);

        internal static string Trunc(this string value) => value.Length > _TRUNCATE_BODY_LENGTH ? $"{value.Substring(0, _TRUNCATE_BODY_LENGTH)}..." : value;
    }
}