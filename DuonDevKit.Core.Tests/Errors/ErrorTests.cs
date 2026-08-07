using System.Reflection;
using DuonDevKit.Core.Errors;

namespace DuonDevKit.Core.Tests.Errors
{
    public class ErrorTests
    {
        [Fact]
        public void Constructor_WithTypeCodeMessage_IsNotPubliclyAccessible()
        {
            // Type.GetConstructor(Type[]) without BindingFlags only finds PUBLIC instance constructors.
            var publicConstructor = typeof(Error).GetConstructor(
                [typeof(ErrorType), typeof(string), typeof(string)]);

            Assert.Null(publicConstructor);
        }

        [Fact]
        public void Constructor_WithTypeCodeMessage_IsPrivate_SoCallersCannotBypassFactories()
        {
            var constructor = typeof(Error).GetConstructor(
                BindingFlags.NonPublic | BindingFlags.Instance,
                binder: null,
                types: [typeof(ErrorType), typeof(string), typeof(string)],
                modifiers: null);

            Assert.NotNull(constructor);
            Assert.True(constructor!.IsPrivate);
        }

        [Fact]
        public void None_HasErrorTypeNone_AndEmptyCodeAndMessage()
        {
            Assert.Equal(ErrorType.None, Error.None.Type);
            Assert.Equal(string.Empty, Error.None.Code);
            Assert.Equal(string.Empty, Error.None.Message);
        }

        [Fact]
        public void Validation_CreatesErrorWithValidationType()
        {
            var error = Error.Validation("VAL001", "Invalid input.");

            Assert.Equal(ErrorType.Validation, error.Type);
            Assert.Equal("VAL001", error.Code);
            Assert.Equal("Invalid input.", error.Message);
        }

        [Fact]
        public void Business_CreatesErrorWithBusinessType()
        {
            var error = Error.Business("BIZ001", "Business rule violated.");

            Assert.Equal(ErrorType.Business, error.Type);
            Assert.Equal("BIZ001", error.Code);
            Assert.Equal("Business rule violated.", error.Message);
        }

        [Fact]
        public void NotFound_CreatesErrorWithNotFoundType()
        {
            var error = Error.NotFound("NF001", "Resource not found.");

            Assert.Equal(ErrorType.NotFound, error.Type);
            Assert.Equal("NF001", error.Code);
            Assert.Equal("Resource not found.", error.Message);
        }

        [Fact]
        public void Conflict_CreatesErrorWithConflictType()
        {
            var error = Error.Conflict("CON001", "Duplicate resource.");

            Assert.Equal(ErrorType.Conflict, error.Type);
            Assert.Equal("CON001", error.Code);
            Assert.Equal("Duplicate resource.", error.Message);
        }

        [Fact]
        public void Unauthorized_CreatesErrorWithUnauthorizedType()
        {
            var error = Error.Unauthorized("UNA001", "Missing credentials.");

            Assert.Equal(ErrorType.Unauthorized, error.Type);
            Assert.Equal("UNA001", error.Code);
            Assert.Equal("Missing credentials.", error.Message);
        }

        [Fact]
        public void Forbidden_CreatesErrorWithForbiddenType()
        {
            var error = Error.Forbidden("FOR001", "Not allowed.");

            Assert.Equal(ErrorType.Forbidden, error.Type);
            Assert.Equal("FOR001", error.Code);
            Assert.Equal("Not allowed.", error.Message);
        }

        [Fact]
        public void Unexpected_CreatesErrorWithUnexpectedType()
        {
            var error = Error.Unexpected("UNX001", "Something went wrong.");

            Assert.Equal(ErrorType.Unexpected, error.Type);
            Assert.Equal("UNX001", error.Code);
            Assert.Equal("Something went wrong.", error.Message);
        }
    }
}
