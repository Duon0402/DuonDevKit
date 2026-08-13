using System.Linq.Expressions;

namespace DuonDevKit.EntityFrameworkCore.Specifications
{
    /// <summary>
    /// A reusable, named query shape — filter, eager-loaded navigation properties, and ordering —
    /// for <see cref="Repositories.IRepository{T}"/>'s <c>ISpecification&lt;T&gt;</c> overloads, so a
    /// recurring query doesn't need its <c>filter</c>/<c>include</c>/<c>orderBy</c> lambdas repeated
    /// (and kept in sync) at every call site. Implement via <see cref="Specification{T}"/> rather than
    /// directly.
    /// </summary>
    public interface ISpecification<T>
    {
        /// <summary>The filter applied via <c>Where</c>, or <c>null</c> to match every entity.</summary>
        Expression<Func<T, bool>>? Criteria { get; }

        /// <summary>Eager-loads navigation properties (e.g. <c>q =&gt; q.Include(x =&gt; x.Customer)</c>), applied in order.</summary>
        IReadOnlyList<Func<IQueryable<T>, IQueryable<T>>> Includes { get; }

        /// <summary>The ordering applied via <c>OrderBy</c>/<c>OrderByDescending</c>, or <c>null</c> for no explicit order.</summary>
        Func<IQueryable<T>, IOrderedQueryable<T>>? OrderBy { get; }
    }
}
