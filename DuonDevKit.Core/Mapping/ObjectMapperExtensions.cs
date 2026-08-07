namespace DuonDevKit.Core.Mapping
{
    /// <summary>Convenience call-site syntax over <see cref="IObjectMapper"/>, e.g. <c>order.MapTo&lt;OrderDto&gt;(mapper)</c>.</summary>
    public static class ObjectMapperExtensions
    {
        /// <summary>Equivalent to <c>mapper.Map&lt;TSource, TDestination&gt;(source)</c>.</summary>
        public static TDestination MapTo<TSource, TDestination>(this TSource source, IObjectMapper mapper)
            => mapper.Map<TSource, TDestination>(source);

        /// <summary>Equivalent to <c>mapper.Map(source, destination)</c>.</summary>
        public static void MapTo<TSource, TDestination>(this TSource source, TDestination destination, IObjectMapper mapper)
            => mapper.Map(source, destination);

        /// <summary>Equivalent to <c>mapper.MapList&lt;TSource, TDestination&gt;(source)</c>.</summary>
        public static List<TDestination> MapToList<TSource, TDestination>(this IEnumerable<TSource> source, IObjectMapper mapper)
            => mapper.MapList<TSource, TDestination>(source);
    }
}
