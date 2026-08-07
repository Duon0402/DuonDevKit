using DuonDevKit.Core.Errors;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace DuonDevKit.AspNetCore
{
    /// <summary>Registers a global handler that turns unhandled exceptions into the same response shape as a failed <see cref="Core.Results.Result"/>.</summary>
    public static class ApplicationBuilderExtensions
    {
        /// <summary>
        /// Catches any exception that reaches the middleware pipeline and writes it as the same
        /// <see cref="Microsoft.AspNetCore.Mvc.ProblemDetails"/> shape
        /// <see cref="ResultExtensions.ToApiResult(Core.Results.Result)"/> uses for a failed <c>Result</c>
        /// (<c>500</c>, <c>Error.Unexpected</c>) — so a bug surfaces the same way an expected failure would,
        /// instead of leaking the framework's default error response. Logs the original exception via
        /// <see cref="ILoggerFactory"/> before responding. Register early in the pipeline, before routing/MVC.
        /// </summary>
        public static IApplicationBuilder UseDuonDevKitExceptionHandling(this IApplicationBuilder app)
            => app.UseExceptionHandler(errorApp => errorApp.Run(async context =>
            {
                var exception = context.Features.Get<IExceptionHandlerFeature>()?.Error;
                if (exception is not null)
                {
                    context.RequestServices.GetService<ILoggerFactory>()
                        ?.CreateLogger("DuonDevKit.AspNetCore.ExceptionHandling")
                        .LogError(exception, "Unhandled exception.");
                }

                var error = Error.Unexpected(ErrorCodes.UnhandledException, "An unexpected error occurred.");
                await ResultExtensions.ToProblemApiResult(error).ExecuteAsync(context);
            }));
    }
}
