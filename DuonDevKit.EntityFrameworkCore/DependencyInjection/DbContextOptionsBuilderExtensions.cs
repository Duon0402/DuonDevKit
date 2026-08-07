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
        /// </summary>
        public static DbContextOptionsBuilder AddDuonDevKitAuditing(this DbContextOptionsBuilder optionsBuilder, IServiceProvider serviceProvider)
        {
            var currentUserProvider = serviceProvider.GetService<ICurrentUserProvider>() ?? new NullCurrentUserProvider();
            return optionsBuilder.AddInterceptors(new AuditSaveChangesInterceptor(currentUserProvider));
        }
    }
}
