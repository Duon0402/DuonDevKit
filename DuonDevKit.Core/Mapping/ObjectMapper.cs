using System.Collections.Concurrent;
using Microsoft.Extensions.DependencyInjection;

namespace DuonDevKit.Core.Mapping
{
    /// <summary>
    /// Default <see cref="IObjectMapper"/>, resolving each type pair's mapper from DI and caching the
    /// resolved instance per type pair for the lifetime of this <see cref="ObjectMapper"/> (typically one
    /// scope/request), so repeated calls for the same pair — including a naive per-item loop calling
    /// <see cref="Map{TSource, TDestination}(TSource)"/> instead of <see cref="MapList{TSource, TDestination}"/>
    /// — only pay the DI lookup once. Assumes mapper implementations are stateless, as they should be.
    /// </summary>
    public sealed class ObjectMapper(IServiceProvider serviceProvider) : IObjectMapper
    {
        private readonly ConcurrentDictionary<(Type Source, Type Destination), object> _mappers = new();
        private readonly ConcurrentDictionary<(Type Source, Type Destination), object> _updateMappers = new();

        /// <inheritdoc />
        public TDestination Map<TSource, TDestination>(TSource source)
            => GetMapper<TSource, TDestination>().Map(source);

        /// <inheritdoc />
        public void Map<TSource, TDestination>(TSource source, TDestination destination)
            => GetUpdateMapper<TSource, TDestination>().Map(source, destination);

        /// <inheritdoc />
        public List<TDestination> MapList<TSource, TDestination>(IEnumerable<TSource> source)
        {
            var mapper = GetMapper<TSource, TDestination>();
            var result = new List<TDestination>(source is ICollection<TSource> collection ? collection.Count : 0);

            foreach (var item in source)
                result.Add(mapper.Map(item));

            return result;
        }

        private IMapper<TSource, TDestination> GetMapper<TSource, TDestination>()
            => (IMapper<TSource, TDestination>)_mappers.GetOrAdd(
                (typeof(TSource), typeof(TDestination)),
                static (_, sp) => sp.GetRequiredService<IMapper<TSource, TDestination>>(),
                serviceProvider)!;

        private IUpdateMapper<TSource, TDestination> GetUpdateMapper<TSource, TDestination>()
            => (IUpdateMapper<TSource, TDestination>)_updateMappers.GetOrAdd(
                (typeof(TSource), typeof(TDestination)),
                static (_, sp) => sp.GetRequiredService<IUpdateMapper<TSource, TDestination>>(),
                serviceProvider)!;
    }
}
