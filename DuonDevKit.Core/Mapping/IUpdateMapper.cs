namespace DuonDevKit.Core.Mapping
{
    /// <summary>
    /// Explicit mapping of <typeparamref name="TSource"/>'s values onto an existing
    /// <typeparamref name="TDestination"/> instance — e.g. applying an update request onto an
    /// already-tracked EF entity. Separate from <see cref="IMapper{TSource, TDestination}"/> since not
    /// every type pair needs an in-place update (a read-only response DTO, for instance, never does).
    /// </summary>
    public interface IUpdateMapper<in TSource, in TDestination>
    {
        /// <summary>Applies <paramref name="source"/>'s values onto <paramref name="destination"/> in place.</summary>
        void Map(TSource source, TDestination destination);
    }
}
