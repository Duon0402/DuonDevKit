namespace DuonDevKit.Core.Mapping
{
    /// <summary>
    /// Explicit mapping from <typeparamref name="TSource"/> to a new <typeparamref name="TDestination"/>.
    /// Implement this per type pair — plain, hand-written C#, no reflection/convention magic like
    /// AutoMapper's. Register implementations via <see cref="MapperServiceCollectionExtensions.AddDuonDevKitMappers"/>.
    /// </summary>
    public interface IMapper<in TSource, out TDestination>
    {
        /// <summary>Creates a new <typeparamref name="TDestination"/> from <paramref name="source"/>.</summary>
        TDestination Map(TSource source);
    }
}
