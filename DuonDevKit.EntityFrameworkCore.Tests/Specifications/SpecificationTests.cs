namespace DuonDevKit.EntityFrameworkCore.Tests.Specifications
{
    public class SpecificationTests
    {
        [Fact]
        public void NoBuilderMethodsCalled_HasNoCriteriaIncludesOrOrderBy()
        {
            var spec = new AllTestEntitiesSpec();

            Assert.Null(spec.Criteria);
            Assert.Empty(spec.Includes);
            Assert.Null(spec.OrderBy);
        }

        [Fact]
        public void AddCriteriaAndApplyOrderBy_SetsBothProperties()
        {
            var spec = new TestEntityByNameSpec("A");

            Assert.NotNull(spec.Criteria);
            Assert.NotNull(spec.OrderBy);
        }

        [Fact]
        public void AddInclude_AppendsToIncludes()
        {
            var spec = new BlogPostsWithCommentsSpec();

            Assert.Single(spec.Includes);
        }
    }
}
