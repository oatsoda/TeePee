namespace TeePee
{
    public enum TeePeeMode
    {
        /// <summary>
        /// Any requests that don't match those seeded in TeePee will return the default result (404, but configurable with <c>TeePeeBuilder.WithDefaultResponse()</c>).
        /// </summary>
        Lenient = 0,

        /// <summary>
        /// Any requests that don't match those seeded in TeePee will throw an exception.
        /// </summary>
        Strict
    }
}