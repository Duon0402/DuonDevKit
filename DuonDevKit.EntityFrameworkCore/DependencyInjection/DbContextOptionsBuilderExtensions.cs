using DuonDevKit.EntityFrameworkCore.Auditing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace DuonDevKit.EntityFrameworkCore.DependencyInjection
{
    /// <summary>Wires this library's EF Core interceptors into a <see cref="DbContextOptionsBuilder"/>.</summary>
    public static class DbContextOptionsBuilderExtensions
    {
        /// <summary>
        /// Adds <see cref="AuditSaveChangesInterceptor"/>, resolving <see cref="ICurrentUserProvider"/> from
        /// <paramref name="serviceProvider"/> (falling back to <see cref="NullCurrentUserProvider"/> if none
        /// is registered). Call from the <c>(sp, options) =&gt;</c> overload of <c>AddDbContext</c>:
        /// <c>services.AddDbContext&lt;AppDbContext&gt;((sp, options) =&gt; options.UseSqlServer(...).AddDuonDevKitAuditing(sp));</c>
        /// <para>
        /// ⚠️ Do not use with <c>AddDbContextPool</c>. Pooled contexts build their options once, at pool
        /// creation, from a bootstrap scope — so the <see cref="ICurrentUserProvider"/> resolved here is
        /// captured once and reused by every pooled context instance for the app's lifetime, silently
        /// stamping every later <c>SaveChanges</c> with whatever user was current at startup instead of
        /// the actual caller. Use <c>AddDbContext</c> (unpooled) for auditing, or resolve the current user
        /// through an ambient mechanism that isn't tied to DI scoping (e.g. an <see cref="System.Threading.AsyncLocal{T}"/>
        /// set by request middleware) if pooling is required.
        /// </para>
        /// </summary>
        public static DbContextOptionsBuilder AddDuonDevKitAuditing(this DbContextOptionsBuilder optionsBuilder, IServiceProvider serviceProvider)
        {
            var currentUserProvider = serviceProvider.GetService<ICurrentUserProvider>() ?? new NullCurrentUserProvider();
            return optionsBuilder.AddInterceptors(new AuditSaveChangesInterceptor(currentUserProvider));
        }
    }
}
