using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;

namespace DuonDevKit.AspNetCore.Validation
{
    /// <summary>
    /// Minimal API <see cref="IEndpointFilter"/> that runs <see cref="System.ComponentModel.DataAnnotations"/>
    /// validation against a bound parameter of type <typeparamref name="T"/> before the endpoint handler
    /// runs, short-circuiting with a <c>400</c> <see cref="ValidationProblem"/> (field name → messages) if
    /// invalid. Register via <see cref="EndpointFilterExtensions.WithDuonDevKitValidation{T}"/> rather than
    /// directly — no dependency beyond the base class library, unlike <c>DuonDevKit.Validation</c>'s
    /// FluentValidation integration; use that instead for rules that need to be conditional, compare
    /// properties against each other, or call out to a database/service.
    /// </summary>
    /// <remarks>
    /// Only <typeparamref name="T"/>'s own properties are checked, not nested complex properties or
    /// collections of sub-objects — see <see cref="DuonDevKit.Core.Validation.DataAnnotationsValidator"/>'s
    /// remarks (this filter runs the same underlying <see cref="Validator"/> call). If no bound argument
    /// of type <typeparamref name="T"/> is found on the request at all (e.g. this filter was attached to
    /// an endpoint whose handler doesn't take a <typeparamref name="T"/> parameter, or the framework
    /// already bound a body-less request to <c>null</c> before this filter ran), the request passes
    /// through unvalidated rather than being treated as a validation failure — pair this filter only with
    /// an endpoint that actually binds a required <typeparamref name="T"/> parameter.
    /// </remarks>
    public sealed class ValidationFilter<T> : IEndpointFilter where T : class
    {
        /// <inheritdoc />
        public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
        {
            var argument = context.Arguments.OfType<T>().FirstOrDefault();
            if (argument is null)
                return await next(context);

            var validationContext = new ValidationContext(argument);
            var results = new List<ValidationResult>();

            if (Validator.TryValidateObject(argument, validationContext, results, validateAllProperties: true))
                return await next(context);

            var errors = results
                .SelectMany(r => r.MemberNames.DefaultIfEmpty(string.Empty), (r, memberName) => (memberName, r.ErrorMessage))
                .GroupBy(x => x.memberName, x => x.ErrorMessage ?? "Invalid value.")
                .ToDictionary(g => g.Key, g => g.ToArray());

            return TypedResults.ValidationProblem(
                errors,
                title: "Validation",
                extensions: new Dictionary<string, object?> { [ResultExtensions.ErrorCodeExtensionKey] = ErrorCodes.ValidationFailed });
        }
    }
}
