namespace DuonDevKit.EntityFrameworkCore.Auditing
{
    /// <summary>
    /// Default <see cref="ICurrentUserProvider"/> registered by
    /// <see cref="DependencyInjection.ServiceCollectionExtensions.AddDuonDevKitEntityFrameworkCore{TContext}"/>
    /// when the consuming app hasn't registered its own — audit fields still get filled with
    /// <c>UserId = null</c> (e.g. for background jobs with no acting user) instead of the setup
    /// failing for lack of an <see cref="ICurrentUserProvider"/> registration.
    /// </summary>
    public sealed class NullCurrentUserProvider : ICurrentUserProvider
    {
        /// <inheritdoc />
        public string? UserId => null;
    }
}
