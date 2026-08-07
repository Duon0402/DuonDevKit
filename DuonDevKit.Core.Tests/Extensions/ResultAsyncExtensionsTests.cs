using DuonDevKit.Core.Errors;
using DuonDevKit.Core.Extensions;
using DuonDevKit.Core.Results;

namespace DuonDevKit.Core.Tests.Extensions
{
    public class ResultAsyncExtensionsTests
    {
        [Fact]
        public async Task MapAsync_SyncMapper_OnSuccess_TransformsValue()
        {
            var resultTask = Task.FromResult(Result.Success(10));

            var mapped = await resultTask.MapAsync(v => v.ToString());

            Assert.True(mapped.IsSuccess);
            Assert.Equal("10", mapped.Value);
        }

        [Fact]
        public async Task MapAsync_SyncMapper_OnFailure_PropagatesError_AndDoesNotInvokeMapper()
        {
            var error = Error.Validation("VAL001", "Invalid input.");
            var resultTask = Task.FromResult(Result.Fail<int>(error));
            var mapperInvoked = false;

            var mapped = await resultTask.MapAsync(v =>
            {
                mapperInvoked = true;
                return v.ToString();
            });

            Assert.True(mapped.IsFailure);
            Assert.Equal(error, mapped.Error);
            Assert.False(mapperInvoked);
        }

        [Fact]
        public async Task MapAsync_AsyncMapper_OnSuccess_TransformsValue()
        {
            var resultTask = Task.FromResult(Result.Success(10));

            var mapped = await resultTask.MapAsync(async v =>
            {
                await Task.Yield();
                return v.ToString();
            });

            Assert.True(mapped.IsSuccess);
            Assert.Equal("10", mapped.Value);
        }

        [Fact]
        public async Task MapAsync_AsyncMapper_OnFailure_PropagatesError_AndDoesNotInvokeMapper()
        {
            var error = Error.NotFound("NF001", "Not found.");
            var resultTask = Task.FromResult(Result.Fail<int>(error));
            var mapperInvoked = false;

            var mapped = await resultTask.MapAsync(async v =>
            {
                mapperInvoked = true;
                await Task.Yield();
                return v.ToString();
            });

            Assert.True(mapped.IsFailure);
            Assert.Equal(error, mapped.Error);
            Assert.False(mapperInvoked);
        }

        [Fact]
        public async Task BindAsync_SyncBinder_OnSuccess_ReturnsBinderResult_WithoutDoubleWrapping()
        {
            var resultTask = Task.FromResult(Result.Success(10));

            Result<string> bound = await resultTask.BindAsync(v => Result.Success(v.ToString()));

            Assert.True(bound.IsSuccess);
            Assert.Equal("10", bound.Value);
        }

        [Fact]
        public async Task BindAsync_SyncBinder_OnFailure_PropagatesOriginalError_AndDoesNotInvokeBinder()
        {
            var error = Error.Unauthorized("UNA001", "Missing credentials.");
            var resultTask = Task.FromResult(Result.Fail<int>(error));
            var binderInvoked = false;

            var bound = await resultTask.BindAsync(v =>
            {
                binderInvoked = true;
                return Result.Success(v.ToString());
            });

            Assert.True(bound.IsFailure);
            Assert.Equal(error, bound.Error);
            Assert.False(binderInvoked);
        }

        [Fact]
        public async Task BindAsync_AsyncBinder_OnSuccess_ReturnsBinderResult_WithoutDoubleWrapping()
        {
            var resultTask = Task.FromResult(Result.Success(10));

            Result<string> bound = await resultTask.BindAsync(async v =>
            {
                await Task.Yield();
                return Result.Success(v.ToString());
            });

            Assert.True(bound.IsSuccess);
            Assert.Equal("10", bound.Value);
        }

        [Fact]
        public async Task BindAsync_AsyncBinder_OnSuccess_WhenBinderFails_PropagatesTheBinderFailure()
        {
            var resultTask = Task.FromResult(Result.Success(10));
            var binderError = Error.Business("BIZ001", "Rule violated.");

            var bound = await resultTask.BindAsync(async v =>
            {
                await Task.Yield();
                return Result.Fail<string>(binderError);
            });

            Assert.True(bound.IsFailure);
            Assert.Equal(binderError, bound.Error);
        }

        [Fact]
        public async Task BindAsync_AsyncBinder_OnFailure_PropagatesOriginalError_AndDoesNotInvokeBinder()
        {
            var error = Error.Forbidden("FOR001", "Not allowed.");
            var resultTask = Task.FromResult(Result.Fail<int>(error));
            var binderInvoked = false;

            var bound = await resultTask.BindAsync(async v =>
            {
                binderInvoked = true;
                await Task.Yield();
                return Result.Success(v.ToString());
            });

            Assert.True(bound.IsFailure);
            Assert.Equal(error, bound.Error);
            Assert.False(binderInvoked);
        }
    }
}
