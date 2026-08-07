using System.Net;
using DuonDevKit.Core.Errors;
using DuonDevKit.Core.Extensions;

namespace DuonDevKit.Core.Tests.Extensions
{
    public class ErrorExtensionsTests
    {
        [Fact]
        public void ToHttpStatusCode_ForNone_ReturnsOK()
        {
            Assert.Equal(HttpStatusCode.OK, Error.None.ToHttpStatusCode());
        }

        [Fact]
        public void ToHttpStatusCode_ForValidation_ReturnsBadRequest()
        {
            var error = Error.Validation("VAL001", "Invalid input.");

            Assert.Equal(HttpStatusCode.BadRequest, error.ToHttpStatusCode());
        }

        [Fact]
        public void ToHttpStatusCode_ForNotFound_ReturnsNotFound()
        {
            var error = Error.NotFound("NF001", "Not found.");

            Assert.Equal(HttpStatusCode.NotFound, error.ToHttpStatusCode());
        }

        [Fact]
        public void ToHttpStatusCode_ForConflict_ReturnsConflict()
        {
            var error = Error.Conflict("CON001", "Duplicate.");

            Assert.Equal(HttpStatusCode.Conflict, error.ToHttpStatusCode());
        }

        [Fact]
        public void ToHttpStatusCode_ForUnauthorized_ReturnsUnauthorized()
        {
            var error = Error.Unauthorized("UNA001", "Missing credentials.");

            Assert.Equal(HttpStatusCode.Unauthorized, error.ToHttpStatusCode());
        }

        [Fact]
        public void ToHttpStatusCode_ForForbidden_ReturnsForbidden()
        {
            var error = Error.Forbidden("FOR001", "Not allowed.");

            Assert.Equal(HttpStatusCode.Forbidden, error.ToHttpStatusCode());
        }

        [Fact]
        public void ToHttpStatusCode_ForBusiness_ReturnsUnprocessableEntity()
        {
            var error = Error.Business("BIZ001", "Rule violated.");

            Assert.Equal(HttpStatusCode.UnprocessableEntity, error.ToHttpStatusCode());
        }

        [Fact]
        public void ToHttpStatusCode_ForUnexpected_ReturnsInternalServerError()
        {
            var error = Error.Unexpected("UNX001", "Something went wrong.");

            Assert.Equal(HttpStatusCode.InternalServerError, error.ToHttpStatusCode());
        }
    }
}
