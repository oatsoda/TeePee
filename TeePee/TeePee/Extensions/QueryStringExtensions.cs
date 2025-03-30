namespace TeePee.Extensions
{
    internal static class QueryStringExtensions
    {
        internal static string RemoveQueryString(this string url)
        {
            return url.Split('?')[0];
        }

        internal static string RemoveQueryString(this Uri uri)
        {
            return uri.ToString().RemoveQueryString();
        }
    }
}