using System.Reflection;
using DuonDevKit.Core.Errors;
using DuonDevKit.Core.Results;

namespace DuonDevKit.Core.Tests.Results
{
    public class ResultTests
    {
        [Fact]
        public void Success_ProducesResultWithIsSuccessTrue_AndErrorNone()
        {
            var result = Result.Success();

            Assert.True(result.IsSuccess);
            Assert.False(result.IsFailure);
            Assert.Equal(Error.None, result.Error);
        }

        [Fact]
        public void Fail_ProducesResultWithIsSuccessFalse_AndGivenError()
        {
            var error = Error.Validation("VAL001", "Invalid input.");

            var result = Result.Fail(error);

            Assert.False(result.IsSuccess);
            Assert.True(result.IsFailure);
            Assert.Equal(error, result.Error);
        }

        [Fact]
        public void Fail_WithNullError_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => Result.Fail(null!));
        }

        [Fact]
        public void Fail_WithErrorNone_ThrowsArgumentException_BecauseFailureMustCarryAnError()
        {
            // A failure result must contain a non-None error; the private ctor's invariant
            // check is reachable through the public Fail factory when misused this way.
            Assert.Throws<ArgumentException>(() => Result.Fail(Error.None));
        }

        [Fact]
        public void Ctor_SuccessWithNonNoneError_ThrowsArgumentException()
        {
            // Result's ctor is private; the "success cannot carry an error" invariant has no
            // public factory path that can misuse it, so it is exercised via reflection.
            var ctor = typeof(Result).GetConstructor(
                BindingFlags.NonPublic | BindingFlags.Instance,
                binder: null,
                types: [typeof(bool), typeof(Error)],
                modifiers: null)!;

            var error = Error.Validation("VAL001", "Invalid input.");

            var ex = Assert.Throws<TargetInvocationException>(() => ctor.Invoke([true, error]));
            Assert.IsType<ArgumentException>(ex.InnerException);
        }

        [Fact]
        public void FailOfT_CreatesFailedResultOfT_CarryingTheGivenError()
        {
            var error = Error.NotFound("NF001", "Not found.");

            Result<string> result = Result.Fail<string>(error);

            Assert.True(result.IsFailure);
            Assert.Equal(error, result.Error);
        }

        [Fact]
        public void Match_OnSuccess_InvokesOnSuccessBranch()
        {
            var result = Result.Success();

            var output = result.Match(
                onSuccess: () => "success",
                onFailure: _ => "failure");

            Assert.Equal("success", output);
        }

        [Fact]
        public void Match_OnFailure_InvokesOnFailureBranchWithError()
        {
            var error = Error.Conflict("CON001", "Duplicate.");
            var result = Result.Fail(error);

            var output = result.Match(
                onSuccess: () => "success",
                onFailure: e => e.Code);

            Assert.Equal("CON001", output);
        }

        [Fact]
        public void ImplicitOperator_FromError_ProducesFailedResult()
        {
            var error = Error.Business("BIZ001", "Rule violated.");

            Result result = error;

            Assert.True(result.IsFailure);
            Assert.Equal(error, result.Error);
        }

        [Fact]
        public void ToString_OnSuccess_ReturnsSuccess()
        {
            Assert.Equal("Success", Result.Success().ToString());
        }

        [Fact]
        public void ToString_OnFailure_IncludesCodeAndMessage()
        {
            var result = Result.Fail(Error.Validation("VAL001", "Invalid input."));

            Assert.Equal("Failure: VAL001 - Invalid input.", result.ToString());
        }
    }
}
