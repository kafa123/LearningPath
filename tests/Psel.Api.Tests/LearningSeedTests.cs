using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Psel.Infrastructure.Persistence;
using Psel.Modules.Learning.Domain;

namespace Psel.Api.Tests;

public sealed class LearningSeedTests
{
    [Fact]
    public async Task Startup_SeedsTheCompleteLearningCatalogOnce()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var factory = new CustomWebApplicationFactory();
        using var client = factory.CreateClient();

        using var firstResponse = await client.GetAsync("/api/domains", cancellationToken);
        firstResponse.EnsureSuccessStatusCode();

        await using var scope = factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<PselDbContext>();

        Assert.Equal(5, await dbContext.Set<LearningDomain>().CountAsync(cancellationToken));
        Assert.Equal(12, await dbContext.Set<LearningModule>().CountAsync(cancellationToken));
        Assert.Equal(12, await dbContext.Set<LearningMaterial>().CountAsync(cancellationToken));
        Assert.Equal(24, await dbContext.Set<Competency>().CountAsync(cancellationToken));
        Assert.Equal(36, await dbContext.Set<LearningObjective>().CountAsync(cancellationToken));
        Assert.Equal(
            7,
            await dbContext.Set<LearningModulePrerequisite>().CountAsync(cancellationToken));

        using var secondResponse = await client.GetAsync("/api/domains", cancellationToken);
        secondResponse.EnsureSuccessStatusCode();

        Assert.Equal(5, await dbContext.Set<LearningDomain>().CountAsync(cancellationToken));
    }
}
