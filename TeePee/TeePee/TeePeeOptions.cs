using System.Text.Json;
using System.Text.Json.Serialization;

namespace TeePee
{
    public class TeePeeOptions
    {        
        // ReSharper disable once MemberCanBePrivate.Global
        public static JsonSerializerOptions DefaultSerializeOptions { get; } = new()
                                                                               {
                                                                                   PropertyNamingPolicy = null,
                                                                                   Converters =
                                                                                   {
                                                                                       new JsonStringEnumConverter()
                                                                                   }
                                                                               };

        /// <summary>
        /// The <see cref="JsonSerializerOptions"/> to use when creating the Response JSON Body from the defined response body object.
        /// </summary>
        public JsonSerializerOptions ResponseBodySerializerOptions { get; set; } = DefaultSerializeOptions;
        
        /// <summary>
        /// The <see cref="JsonSerializerOptions"/> to use when creating the expected Request JSON - to match with - from the defined request body object.
        /// </summary>
        public JsonSerializerOptions RequestBodySerializerOptions { get; set; } = DefaultSerializeOptions;

        /// <summary>
        /// Whether matching is case sensitive. Default is False. Used for all matching: URL, Query Params, Body and Media Types.
        /// </summary>
        public bool CaseSensitiveMatching { get; set; }

        /// <summary>
        /// The max characters of the JSON body to write to Exception messages or log outputs. Default is 500.
        /// </summary>
        public int? TruncateBodyOutputLength { get; set; } = 500;

        /// <summary>
        /// Whether to output full verbose details of match failures.
        /// </summary>
        public bool ShowFullDetailsOnMatchFailure { get; set; }

        /// <summary>
        /// The Mode for TeePee. See <see cref="TeePeeMode"/>. Default is Lenient.
        /// </summary>
        public TeePeeMode Mode { get; set; } = TeePeeMode.Lenient;

        /// <summary>
        /// The BuilderMode for TeePee. See <see cref="TeePeeBuilderMode"/>. Default is AllowMultipleUrlRules.
        /// </summary>
        public TeePeeBuilderMode BuilderMode { get; set; } = TeePeeBuilderMode.AllowMultipleUrlRules;
    }
}