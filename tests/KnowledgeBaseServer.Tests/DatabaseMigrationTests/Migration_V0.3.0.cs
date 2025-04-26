using Xunit;

namespace KnowledgeBaseServer.Tests.DatabaseMigrationTests;

// ReSharper disable once InconsistentNaming
public class Migration_V0_3_0
{
    [Fact(DisplayName = "v0.3.0 did not include schema changes")]
    public void ShouldMigrateToLatest()
    {
        // arrange

        // act

        // assert
        Assert.True(true);
    }
}
