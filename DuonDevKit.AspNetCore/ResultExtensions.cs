using DuonDevKit.Core.Errors;
using DuonDevKit.Core.Extensions;
using DuonDevKit.Core.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace DuonDevKit.AspNetCore
{
    /// <summary>
    /// Maps <see cref="Result"/>/<see cref="Result{T}"/> to ASP.NET Core response types, so a controller
    /// or Minimal API endpoint doesn't need to switch on <see cref="Error.Type"/> itself. A failure becomes
    /// a <see cref="ProblemDetails"/> body using <see cref="ErrorExtensions.ToHttpStatusCode"/> for the
    /// status code, with the error's <see cref="Error.Code"/> exposed as an <c>errorCode</c> extension.
    /// </summary>
    public static class ResultExtensions
    {
        private const string ErrorCodeExtensionKey = "errorCode";

        /// <summary>Maps a successful result to <c>204 No Content</c>, or a failed one to a <see cref="ProblemDetails"/> result.</summary>
        public static IActionResult ToActionResult(this Result result)
            => result.IsSuccess ? new NoContentResult() : ToProblemActionResult(result.Error);

        /// <summary>Maps a successful result to <c>200 OK</c> with <see cref="Result{T}.Value"/> as the body, or a failed one to a <see cref="ProblemDetails"/> result.</summary>
        public static IActionResult ToActionResult<T>(this Result<T> result)
            => result.IsSuccess ? new OkObjectResult(result.Value) : ToProblemActionResult(result.Error);

        /// <summary>Minimal API equivalent of <see cref="ToActionResult(Result)"/>.</summary>
        public static IResult ToApiResult(this Result result)
            => result.IsSuccess ? Results.NoContent() : ToProblemApiResult(result.Error);

        /// <summary>Minimal API equivalent of <see cref="ToActionResult{T}(Result{T})"/>.</summary>
        public static IResult ToApiResult<T>(this Result<T> result)
            => result.IsSuccess ? Results.Ok(result.Value) : ToProblemApiResult(result.Error);

        private static ObjectResult ToProblemActionResult(Error error)
        {
            var problem = ToProblemDetails(error);
            return new ObjectResult(problem) { StatusCode = problem.Status };
        }

        internal static IResult ToProblemApiResult(Error error)
        {
            var problem = ToProblemDetails(error);
            return Results.Problem(
                detail: problem.Detail,
                statusCode: problem.Status,
                title: problem.Title,
                extensions: problem.Extensions);
        }

        /// <summary>
        /// Generic detail shown for <see cref="ErrorType.Unexpected"/> errors instead of
        /// <see cref="Error.Message"/> — that message is frequently a caught exception's raw text
        /// (e.g. <see cref="DuonDevKit.EntityFrameworkCore.UnitOfWork.SaveChangesAsync"/>,
        /// <c>DuonDevKit.Dapper.DapperQueries</c>), which can contain internal details (schema, query
        /// fragments) that shouldn't reach an HTTP client. Mirrors the behavior of
        /// <see cref="ApplicationBuilderExtensions.UseDuonDevKitExceptionHandling"/> for genuinely
        /// unhandled exceptions, so a caught-and-converted-to-Result failure isn't held to a weaker
        /// disclosure standard than an uncaught one. The original message is still on <c>Error.Message</c>
        /// for the caller to log — this only affects what's serialized to the client.
        /// </summary>
        private const string UnexpectedErrorDetail = "An unexpected error occurred.";

        private static ProblemDetails ToProblemDetails(Error error)
        {
            var statusCode = (int)error.ToHttpStatusCode();
            return new ProblemDetails
            {
                Status = statusCode,
                Title = error.Type.ToString(),
                Detail = error.Type == ErrorType.Unexpected ? UnexpectedErrorDetail : error.Message,
                Extensions = { [ErrorCodeExtensionKey] = error.Code },
            };
        }
    }
}
