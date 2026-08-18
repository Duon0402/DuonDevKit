using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.DependencyInjection;

namespace DuonDevKit.AspNetCore.Validation
{
    /// <summary>
    /// Minimal API <see cref="IEndpointFilter"/> that runs a DI-resolved FluentValidation
    /// <see cref="IValidator{T}"/> against a bound parameter of type <typeparamref name="T"/> before the
    /// endpoint handler runs, short-circuiting with a <c>400</c> <see cref="ValidationProblem"/> (field
    /// name → messages) if invalid — the same response shape <see cref="ValidationFilter{T}"/> produces
    /// for DataAnnotations, so either validation style is interchangeable at the Minimal API boundary.
    /// Register via <see cref="EndpointFilterExtensions.WithDuonDevKitFluentValidation{T}"/>.
    /// </summary>
    /// <remarks>
    /// Requires an <see cref="IValidator{T}"/> registered in DI (e.g. via
    /// <c>DuonDevKit.Validation.DependencyInjection.ServiceCollectionExtensions.AddDuonDevKitValidators</c>)
    /// — throws at request time if none is registered, rather than silently skipping validation, since a
    /// missing registration is a setup mistake and not a legitimate "nothing to validate" case. As with
    /// <see cref="ValidationFilter{T}"/>, if no bound argument of type <typeparamref name="T"/> is found
    /// at all, the request passes through unvalidated.
    /// </remarks>
    public sealed class FluentValidationFilter<T> : IEndpointFilter where T : class
    {
        /// <inheritdoc />
        public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
        {
            var argument = context.Arguments.OfType<T>().FirstOrDefault();
            if (argument is null)
                return await next(context);

            var validator = context.HttpContext.RequestServices.GetRequiredService<IValidator<T>>();
            var validationResult = await validator.ValidateAsync(argument, context.HttpContext.RequestAborted);

            var errors = validationResult.Errors
                .Where(e => e.Severity == Severity.Error)
                .GroupBy(e => e.PropertyName, e => e.ErrorMessage)
                .ToDictionary(g => g.Key, g => g.ToArray());

            if (errors.Count == 0)
                return await next(context);

            return TypedResults.ValidationProblem(
                errors,
                title: "Validation",
                extensions: new Dictionary<string, object?> { [ResultExtensions.ErrorCodeExtensionKey] = ErrorCodes.ValidationFailed });
        }
    }
}
