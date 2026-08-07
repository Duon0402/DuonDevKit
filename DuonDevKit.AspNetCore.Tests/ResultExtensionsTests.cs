using DuonDevKit.Core.Errors;
using DuonDevKit.Core.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace DuonDevKit.AspNetCore.Tests
{
    public class ResultExtensionsTests
    {
        [Fact]
        public void ToActionResult_SuccessfulResult_ReturnsNoContent()
        {
            var result = Result.Success();

            var actionResult = result.ToActionResult();

            Assert.IsType<NoContentResult>(actionResult);
        }

        [Fact]
        public void ToActionResult_SuccessfulResultOfT_ReturnsOkWithValue()
        {
            var result = Result.Success(42);

            var actionResult = Assert.IsType<OkObjectResult>(result.ToActionResult());

            Assert.Equal(42, actionResult.Value);
            Assert.Equal(200, actionResult.StatusCode);
        }

        [Fact]
        public void ToActionResult_FailedResult_ReturnsProblemDetailsWithMappedStatusAndErrorCode()
        {
            var error = Error.NotFound("USER001", "User not found.");
            var result = Result.Fail(error);

            var actionResult = Assert.IsType<ObjectResult>(result.ToActionResult());
            var problem = Assert.IsType<ProblemDetails>(actionResult.Value);

            Assert.Equal(404, actionResult.StatusCode);
            Assert.Equal(404, problem.Status);
            Assert.Equal("NotFound", problem.Title);
            Assert.Equal("User not found.", problem.Detail);
            Assert.Equal("USER001", problem.Extensions["errorCode"]);
        }

        [Fact]
        public void ToActionResult_FailedResultOfT_ReturnsProblemDetailsWithMappedStatusAndErrorCode()
        {
            var error = Error.Validation("VAL001", "Name is required.");
            var result = Result.Fail<string>(error);

            var actionResult = Assert.IsType<ObjectResult>(result.ToActionResult());
            var problem = Assert.IsType<ProblemDetails>(actionResult.Value);

            Assert.Equal(400, actionResult.StatusCode);
            Assert.Equal("VAL001", problem.Extensions["errorCode"]);
        }

        [Fact]
        public void ToApiResult_SuccessfulResult_ReturnsNoContent()
        {
            var result = Result.Success();

            var apiResult = Assert.IsAssignableFrom<IStatusCodeHttpResult>(result.ToApiResult());

            Assert.Equal(204, apiResult.StatusCode);
        }

        [Fact]
        public void ToApiResult_SuccessfulResultOfT_ReturnsOkWithValue()
        {
            var result = Result.Success(42);

            var apiResult = Assert.IsType<Ok<int>>(result.ToApiResult());

            Assert.Equal(42, apiResult.Value);
            Assert.Equal(200, apiResult.StatusCode);
        }

        [Fact]
        public void ToApiResult_FailedResult_ReturnsProblemWithMappedStatusAndErrorCode()
        {
            var error = Error.Conflict("ORD001", "Order already shipped.");
            var result = Result.Fail(error);

            var apiResult = Assert.IsType<ProblemHttpResult>(result.ToApiResult());

            Assert.Equal(409, apiResult.StatusCode);
            Assert.Equal(409, apiResult.ProblemDetails.Status);
            Assert.Equal("Conflict", apiResult.ProblemDetails.Title);
            Assert.Equal("Order already shipped.", apiResult.ProblemDetails.Detail);
            Assert.Equal("ORD001", apiResult.ProblemDetails.Extensions["errorCode"]);
        }

        [Fact]
        public void ToApiResult_FailedResultOfT_ReturnsProblemWithMappedStatusAndErrorCode()
        {
            var error = Error.Unexpected("SYS001", "Something went wrong.");
            var result = Result.Fail<int>(error);

            var apiResult = Assert.IsType<ProblemHttpResult>(result.ToApiResult());

            Assert.Equal(500, apiResult.StatusCode);
            Assert.Equal("SYS001", apiResult.ProblemDetails.Extensions["errorCode"]);
        }

        [Theory]
        [InlineData(ErrorTypeCase.Validation, 400)]
        [InlineData(ErrorTypeCase.Business, 422)]
        [InlineData(ErrorTypeCase.NotFound, 404)]
        [InlineData(ErrorTypeCase.Conflict, 409)]
        [InlineData(ErrorTypeCase.Unauthorized, 401)]
        [InlineData(ErrorTypeCase.Forbidden, 403)]
        [InlineData(ErrorTypeCase.Unexpected, 500)]
        public void ToActionResult_EachErrorType_MapsToExpectedStatusCode(ErrorTypeCase errorTypeCase, int expectedStatus)
        {
            Error error = errorTypeCase switch
            {
                ErrorTypeCase.Validation => Error.Validation("E", "m"),
                ErrorTypeCase.Business => Error.Business("E", "m"),
                ErrorTypeCase.NotFound => Error.NotFound("E", "m"),
                ErrorTypeCase.Conflict => Error.Conflict("E", "m"),
                ErrorTypeCase.Unauthorized => Error.Unauthorized("E", "m"),
                ErrorTypeCase.Forbidden => Error.Forbidden("E", "m"),
                ErrorTypeCase.Unexpected => Error.Unexpected("E", "m"),
                _ => throw new ArgumentOutOfRangeException(nameof(errorTypeCase)),
            };

            var actionResult = Assert.IsType<ObjectResult>(Result.Fail(error).ToActionResult());

            Assert.Equal(expectedStatus, actionResult.StatusCode);
        }

        public enum ErrorTypeCase
        {
            Validation,
            Business,
            NotFound,
            Conflict,
            Unauthorized,
            Forbidden,
            Unexpected,
        }
    }
}
