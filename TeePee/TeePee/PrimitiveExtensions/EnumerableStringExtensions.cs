namespace TeePee.Extensions
{
    internal static class EnumerableStringExtensions
    {
        internal static string Flat(this IEnumerable<string> values) => string.Join(',', values);
    }
}