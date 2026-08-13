using System.Linq.Expressions;

namespace DuonDevKit.EntityFrameworkCore.Specifications
{
    /// <summary>
    /// Base class for a named <see cref="ISpecification{T}"/> — subclass it and call
    /// <see cref="AddCriteria"/>/<see cref="AddInclude"/>/<see cref="ApplyOrderBy"/> from the
    /// constructor, e.g. <c>public class ActiveUsersSpec : Specification&lt;User&gt; { public
    /// ActiveUsersSpec() =&gt; AddCriteria(u =&gt; u.IsActive); }</c>.
    /// </summary>
    public abstract class Specification<T> : ISpecification<T>
    {
        private readonly List<Func<IQueryable<T>, IQueryable<T>>> _includes = [];

        /// <inheritdoc />
        public Expression<Func<T, bool>>? Criteria { get; private set; }

        /// <inheritdoc />
        public IReadOnlyList<Func<IQueryable<T>, IQueryable<T>>> Includes => _includes;

        /// <inheritdoc />
        public Func<IQueryable<T>, IOrderedQueryable<T>>? OrderBy { get; private set; }

        /// <summary>Sets the filter applied via <c>Where</c>. Calling this again replaces the previous criteria rather than combining with it.</summary>
        protected void AddCriteria(Expression<Func<T, bool>> criteria)
        {
            ArgumentNullException.ThrowIfNull(criteria);
            Criteria = criteria;
        }

        /// <summary>Adds a navigation property to eager-load, e.g. <c>q =&gt; q.Include(x =&gt; x.Customer)</c>. Applied in the order added.</summary>
        protected void AddInclude(Func<IQueryable<T>, IQueryable<T>> include)
        {
            ArgumentNullException.ThrowIfNull(include);
            _includes.Add(include);
        }

        /// <summary>Sets the ordering applied via <c>OrderBy</c>/<c>OrderByDescending</c>. Calling this again replaces the previous ordering rather than combining with it.</summary>
        protected void ApplyOrderBy(Func<IQueryable<T>, IOrderedQueryable<T>> orderBy)
        {
            ArgumentNullException.ThrowIfNull(orderBy);
            OrderBy = orderBy;
        }
    }
}
