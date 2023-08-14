namespace TeePee
{
    public enum TeePeeBuilderMode
    {
        /// <summary>
        /// Requests seeded in TeePee do not need to have unique URLs.
        /// </summary>
        AllowMultipleUrlRules = 0,

        /// <summary>
        /// Requests seeded in TeePee must have unique URLs.
        /// </summary>
        RequireUniqueUrlRules
    }
}