using System.Reflection;
using DuonDevKit.Core.Errors;
using DuonDevKit.Core.Results;

namespace DuonDevKit.Core.Tests.Results
{
    public class ResultOfTTests
    {
        [Fact]
        public void Success_ProducesResultWithValue_AndErrorNone()
        {
            var result = Result.Success(42);

            Assert.True(result.IsSuccess);
            Assert.False(result.IsFailure);
            Assert.Equal(42, result.Value);
            Assert.Equal(Error.None, result.Error);
        }

        [Fact]
        public void Fail_ProducesResultWithGivenError()
        {
            var error = Error.Validation("VAL001", "Invalid input.");

            var result = Result.Fail<int>(error);

            Assert.False(result.IsSuccess);
            Assert.True(result.IsFailure);
            Assert.Equal(error, result.Error);
        }

        [Fact]
        public void Fail_WithNullError_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => Result.Fail<int>(null!));
        }

        [Fact]
        public void Fail_WithErrorNone_ThrowsArgumentException_BecauseFailureMustCarryAnError()
        {
            Assert.Throws<ArgumentException>(() => Result.Fail<int>(Error.None));
        }

        [Fact]
        public void Ctor_SuccessWithNonNoneError_ThrowsArgumentException()
        {
            // Result<T>'s ctor is internal with no public factory path that can misuse the
            // "success cannot carry an error" invariant, so it is exercised via reflection.
            // T? for an unconstrained T is a nullability annotation only — at runtime the
            // parameter type is T itself (int), not Nullable<int>.
            var ctor = typeof(Result<int>).GetConstructor(
                BindingFlags.NonPublic | BindingFlags.Instance,
                binder: null,
                types: [typeof(bool), typeof(Error), typeof(int)],
                modifiers: null)!;

            var error = Error.Validation("VAL001", "Invalid input.");

            var ex = Assert.Throws<TargetInvocationException>(() => ctor.Invoke([true, error, 1]));
            Assert.IsType<ArgumentException>(ex.InnerException);
        }

        [Fact]
        public void Value_WhenFailure_ThrowsInvalidOperationException()
        {
            var result = Result.Fail<int>(Error.NotFound("NF001", "Not found."));

            Assert.Throws<InvalidOperationException>(() => result.Value);
        }

        [Fact]
        public void Match_OnSuccess_InvokesOnSuccessBranchWithValue()
        {
            var result = Result.Success(10);

            var output = result.Match(
                onSuccess: v => v * 2,
                onFailure: _ => -1);

            Assert.Equal(20, output);
        }

        [Fact]
        public void Match_OnFailure_InvokesOnFailureBranchWithError()
        {
            var error = Error.Conflict("CON001", "Duplicate.");
            var result = Result.Fail<int>(error);

            var output = result.Match(
                onSuccess: v => v.ToString(),
                onFailure: e => e.Code);

            Assert.Equal("CON001", output);
        }

        [Fact]
        public void Map_OnSuccess_TransformsValue()
        {
            var result = Result.Success(10);

            var mapped = result.Map(v => v.ToString());

            Assert.True(mapped.IsSuccess);
            Assert.Equal("10", mapped.Value);
        }

        [Fact]
        public void Map_OnFailure_PropagatesErrorUnchanged_AndDoesNotInvokeMapper()
        {
            var error = Error.Business("BIZ001", "Rule violated.");
            var result = Result.Fail<int>(error);
            var mapperInvoked = false;

            var mapped = result.Map(v =>
            {
                mapperInvoked = true;
                return v.ToString();
            });

            Assert.True(mapped.IsFailure);
            Assert.Equal(error, mapped.Error);
            Assert.False(mapperInvoked);
        }

        [Fact]
        public void Bind_OnSuccess_ReturnsBinderResult_WithoutDoubleWrapping()
        {
            var result = Result.Success(10);

            Result<string> bound = result.Bind(v => Result.Success(v.ToString()));

            Assert.True(bound.IsSuccess);
            Assert.Equal("10", bound.Value);
            // If Bind double-wrapped, this would not compile as Result<string> —
            // the assignment above already proves single-wrapping at compile time.
        }

        [Fact]
        public void Bind_OnSuccess_WhenBinderFails_PropagatesTheBinderFailure()
        {
            var result = Result.Success(10);
            var binderError = Error.Validation("VAL002", "Binder failed.");

            var bound = result.Bind(_ => Result.Fail<string>(binderError));

            Assert.True(bound.IsFailure);
            Assert.Equal(binderError, bound.Error);
        }

        [Fact]
        public void Bind_OnFailure_PropagatesOriginalError_AndDoesNotInvokeBinder()
        {
            var error = Error.Unauthorized("UNA001", "Missing credentials.");
            var result = Result.Fail<int>(error);
            var binderInvoked = false;

            var bound = result.Bind(v =>
            {
                binderInvoked = true;
                return Result.Success(v.ToString());
            });

            Assert.True(bound.IsFailure);
            Assert.Equal(error, bound.Error);
            Assert.False(binderInvoked);
        }

        [Fact]
        public void ImplicitOperator_FromError_ProducesFailedResultOfT()
        {
            var error = Error.Forbidden("FOR001", "Not allowed.");

            Result<int> result = error;

            Assert.True(result.IsFailure);
            Assert.Equal(error, result.Error);
        }

        [Fact]
        public void ImplicitOperator_FromError_ForResultOfError_IsUnambiguous()
        {
            // Regression test for CS0457: previously Result<T> exposed both
            // implicit operator Result<T>(T value) and implicit operator Result<T>(Error error).
            // When T = Error, both operators had the identical signature Error -> Result<Error>,
            // which the compiler rejected as an ambiguous user-defined conversion.
            // Only the Error -> Result<T> operator remains, so this must compile and behave
            // as a failure carrying the given error.
            var error = Error.Unexpected("UNX001", "Something went wrong.");

            Result<Error> result = error;

            Assert.True(result.IsFailure);
            Assert.Equal(error, result.Error);
        }

        [Fact]
        public void ToString_OnSuccess_IncludesValue()
        {
            var result = Result.Success(42);

            Assert.Equal("Success: 42", result.ToString());
        }

        [Fact]
        public void ToString_OnFailure_IncludesCodeAndMessage()
        {
            var result = Result.Fail<int>(Error.Validation("VAL001", "Invalid input."));

            Assert.Equal("Failure: VAL001 - Invalid input.", result.ToString());
        }
    }
}
