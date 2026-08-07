using DuonDevKit.Core.Errors;
using DuonDevKit.Core.Options;

namespace DuonDevKit.Core.Tests.Options
{
    public class OptionOfTTests
    {
        [Fact]
        public void Some_ProducesOptionWithHasValueTrue_AndGivenValue()
        {
            var option = Option<int>.Some(42);

            Assert.True(option.HasValue);
            Assert.False(option.IsNone);
            Assert.Equal(42, option.Value);
        }

        [Fact]
        public void None_ProducesOptionWithHasValueFalse()
        {
            var option = Option<int>.None;

            Assert.False(option.HasValue);
            Assert.True(option.IsNone);
        }

        [Fact]
        public void Some_WithNullValue_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => Option<string>.Some(null!));
        }

        [Fact]
        public void Value_WhenNone_Throws()
        {
            var option = Option<int>.None;

            Assert.Throws<InvalidOperationException>(() => option.Value);
        }

        [Fact]
        public void ImplicitOperator_FromNonNullValue_ProducesSome()
        {
            Option<string> option = "hello";

            Assert.True(option.HasValue);
            Assert.Equal("hello", option.Value);
        }

        [Fact]
        public void ImplicitOperator_FromNullValue_ProducesNone()
        {
            Option<string> option = (string?)null!;

            Assert.True(option.IsNone);
        }

        [Fact]
        public void DefaultOption_IsNone()
        {
            var option = default(Option<int>);

            Assert.True(option.IsNone);
        }

        [Fact]
        public void Match_OnSome_InvokesOnSomeBranchWithValue()
        {
            var option = Option<int>.Some(10);

            var output = option.Match(onSome: v => v * 2, onNone: () => -1);

            Assert.Equal(20, output);
        }

        [Fact]
        public void Match_OnNone_InvokesOnNoneBranch()
        {
            var option = Option<int>.None;

            var output = option.Match(onSome: v => v.ToString(), onNone: () => "none");

            Assert.Equal("none", output);
        }

        [Fact]
        public void Map_OnSome_TransformsValue()
        {
            var option = Option<int>.Some(10);

            var mapped = option.Map(v => v.ToString());

            Assert.True(mapped.HasValue);
            Assert.Equal("10", mapped.Value);
        }

        [Fact]
        public void Map_OnNone_PropagatesNone_AndDoesNotInvokeMapper()
        {
            var option = Option<int>.None;
            var mapperInvoked = false;

            var mapped = option.Map(v =>
            {
                mapperInvoked = true;
                return v.ToString();
            });

            Assert.True(mapped.IsNone);
            Assert.False(mapperInvoked);
        }

        [Fact]
        public void ToResult_OnSome_ReturnsSuccessWithValue()
        {
            var option = Option<int>.Some(10);

            var result = option.ToResult(Error.NotFound("NF001", "Not found."));

            Assert.True(result.IsSuccess);
            Assert.Equal(10, result.Value);
        }

        [Fact]
        public void ToResult_OnNone_ReturnsFailureWithGivenError()
        {
            var option = Option<int>.None;
            var error = Error.NotFound("NF001", "Not found.");

            var result = option.ToResult(error);

            Assert.True(result.IsFailure);
            Assert.Equal(error, result.Error);
        }

        [Fact]
        public void ToString_OnSome_IncludesValue()
        {
            var option = Option<int>.Some(42);

            Assert.Equal("Some: 42", option.ToString());
        }

        [Fact]
        public void ToString_OnNone_ReturnsNone()
        {
            var option = Option<int>.None;

            Assert.Equal("None", option.ToString());
        }
    }
}
