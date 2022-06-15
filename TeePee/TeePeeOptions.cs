using System.Text.Json;
using System.Text.Json.Serialization;

namespace TeePee
{
    public class TeePeeOptions
    {        
        // ReSharper disable once MemberCanBePrivate.Global
        public static JsonSerializerOptions DefaultSerializeOptions { get; } = new JsonSerializerOptions
                                                                               {
                                                                                   PropertyNamingPolicy = null,
                                                                                   Converters =
                                                                                   {
                                                                                       new JsonStringEnumConverter()
                                                                                   }
                                                                               };

        public JsonSerializerOptions ResponseBodySerializerOptions { get; set; } = DefaultSerializeOptions;
        public JsonSerializerOptions RequestBodySerializerOptions { get; set; } = DefaultSerializeOptions;
        public bool CaseSensitiveMatching { get; set; }
        public int? TruncateBodyOutputLength { get; set; } = 200;
        public bool ShowFullDetailsOnMatchFailure { get; set; }
        public TeePeeMode Mode { get; set; } = TeePeeMode.Lenient;
        public TeePeeBuilderMode BuilderMode { get; set; } = TeePeeBuilderMode.AllowMultipleUrlRules;
    }
}