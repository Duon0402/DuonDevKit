using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace DuonDevKit.AspNetCore.Validation
{
    /// <summary>Registers automatic request validation on a Minimal API route.</summary>
    public static class EndpointFilterExtensions
    {
        /// <summary>
        /// Validates the endpoint's bound parameter of type <typeparamref name="T"/> against its
        /// <see cref="System.ComponentModel.DataAnnotations"/> attributes before the handler runs —
        /// short-circuits with a <c>400</c> field-level <c>ValidationProblem</c> if invalid.
        /// </summary>
        /// <example>
        /// <code>
        /// app.MapPost("/orders", Handler).WithDuonDevKitValidation&lt;CreateOrderRequest&gt;();
        /// </code>
        /// </example>
        public static RouteHandlerBuilder WithDuonDevKitValidation<T>(this RouteHandlerBuilder builder) where T : class
            => builder.AddEndpointFilter<ValidationFilter<T>>();
    }
}
