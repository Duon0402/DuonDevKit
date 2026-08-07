using DuonDevKit.Core.Errors;
using System.Net;

namespace DuonDevKit.Core.Extensions
{
    /// <summary>Maps an <see cref="Error"/> to its corresponding HTTP status code.</summary>
    public static class ErrorExtensions
    {
        /// <summary>Returns the HTTP status code that best represents this error's <see cref="ErrorType"/>.</summary>
        public static HttpStatusCode ToHttpStatusCode(this Error error) => error.Type switch
        {
            ErrorType.None => HttpStatusCode.OK,
            ErrorType.Validation => HttpStatusCode.BadRequest,
            ErrorType.NotFound => HttpStatusCode.NotFound,
            ErrorType.Conflict => HttpStatusCode.Conflict,
            ErrorType.Unauthorized => HttpStatusCode.Unauthorized,
            ErrorType.Forbidden => HttpStatusCode.Forbidden,
            ErrorType.Business => HttpStatusCode.UnprocessableEntity,
            ErrorType.Unexpected => HttpStatusCode.InternalServerError,
            _ => HttpStatusCode.InternalServerError,
        };
    }
}
