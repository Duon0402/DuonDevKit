namespace DuonDevKit.Core.Mapping
{
    /// <summary>
    /// Resolves the right <see cref="IMapper{TSource, TDestination}"/>/<see cref="IUpdateMapper{TSource, TDestination}"/>
    /// for a type pair and invokes it — a single injectable facade instead of constructor-injecting each
    /// mapper individually, for callers that map many different type pairs (e.g. a generic CRUD service).
    /// </summary>
    /// <remarks>
    /// The default implementation (<see cref="ObjectMapper"/>) caches the resolved mapper per type pair, so
    /// only the first call for a given pair pays the DI lookup — every call after that, including a naive
    /// per-item loop, is a cached-instance dispatch. Even so, when the type pair is known at compile time,
    /// constructor-injecting <see cref="IMapper{TSource, TDestination}"/>/<see cref="IUpdateMapper{TSource, TDestination}"/>
    /// directly is still marginally cheaper (no dictionary lookup at all) and more explicit about what a
    /// class depends on. Reach for <see cref="IObjectMapper"/> when the type pair varies at the call site
    /// and injecting one interface per pair isn't practical.
    /// </remarks>
    public interface IObjectMapper
    {
        /// <summary>Creates a new <typeparamref name="TDestination"/> from <paramref name="source"/> via the registered <see cref="IMapper{TSource, TDestination}"/>.</summary>
        TDestination Map<TSource, TDestination>(TSource source);

        /// <summary>Applies <paramref name="source"/> onto <paramref name="destination"/> in place via the registered <see cref="IUpdateMapper{TSource, TDestination}"/>.</summary>
        void Map<TSource, TDestination>(TSource source, TDestination destination);

        /// <summary>Maps every item in <paramref name="source"/> via the registered <see cref="IMapper{TSource, TDestination}"/>.</summary>
        List<TDestination> MapList<TSource, TDestination>(IEnumerable<TSource> source);
    }
}
