using DuonDevKit.Validation.DependencyInjection;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace DuonDevKit.Validation.Tests
{
    public class ServiceCollectionExtensionsTests
    {
        [Fact]
        public void AddDuonDevKitValidators_NoAssembliesProvided_Throws()
        {
            var services = new ServiceCollection();

            Assert.Throws<ArgumentException>(() => services.AddDuonDevKitValidators());
        }

        [Fact]
        public void AddDuonDevKitValidators_ExplicitTypeList_RegistersValidatorAsScoped()
        {
            var services = new ServiceCollection();
            services.AddDuonDevKitValidators([typeof(PersonValidator)]);

            using var scope = services.BuildServiceProvider().CreateScope();
            var validator = scope.ServiceProvider.GetRequiredService<IValidator<Person>>();

            Assert.IsType<PersonValidator>(validator);
        }

        [Fact]
        public void AddDuonDevKitValidators_TwoValidatorsForSameType_ThrowsInsteadOfSilentlyPickingOne()
        {
            var services = new ServiceCollection();

            var exception = Assert.Throws<InvalidOperationException>(() => services.AddDuonDevKitValidators(
                [typeof(ProductValidator), typeof(DuplicateProductValidator)]));

            Assert.Contains(nameof(ProductValidator), exception.Message);
            Assert.Contains(nameof(DuplicateProductValidator), exception.Message);
        }

        [Fact]
        public void AddDuonDevKitValidators_CalledTwiceForSameType_ThrowsOnSecondCallToo()
        {
            var services = new ServiceCollection();
            services.AddDuonDevKitValidators([typeof(ProductValidator)]);

            var exception = Assert.Throws<InvalidOperationException>(
                () => services.AddDuonDevKitValidators([typeof(DuplicateProductValidator)]));

            Assert.Contains(nameof(ProductValidator), exception.Message);
            Assert.Contains(nameof(DuplicateProductValidator), exception.Message);
        }

        [Fact]
        public void AddDuonDevKitValidators_WholeAssemblyScan_RejectsTheDeliberateDuplicate()
        {
            var services = new ServiceCollection();

            var exception = Assert.Throws<InvalidOperationException>(
                () => services.AddDuonDevKitValidators(typeof(ProductValidator).Assembly));

            Assert.Contains(nameof(ProductValidator), exception.Message);
        }
    }
}
