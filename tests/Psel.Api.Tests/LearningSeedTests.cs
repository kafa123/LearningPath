using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Psel.Infrastructure.Persistence;
using Psel.Modules.Learning.Domain;
using Psel.Modules.Learning.Seed;

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

        Assert.Equal(6, await dbContext.Set<LearningDomain>().CountAsync(cancellationToken));
        Assert.Equal(19, await dbContext.Set<LearningModule>().CountAsync(cancellationToken));
        Assert.Equal(19, await dbContext.Set<LearningMaterial>().CountAsync(cancellationToken));
        Assert.Equal(38, await dbContext.Set<Competency>().CountAsync(cancellationToken));
        Assert.Equal(57, await dbContext.Set<LearningObjective>().CountAsync(cancellationToken));
        Assert.Equal(
            13,
            await dbContext.Set<LearningModulePrerequisite>().CountAsync(cancellationToken));

        using var secondResponse = await client.GetAsync("/api/domains", cancellationToken);
        secondResponse.EnsureSuccessStatusCode();

        Assert.Equal(6, await dbContext.Set<LearningDomain>().CountAsync(cancellationToken));
    }

    [Fact]
    public async Task Restart_RestoresEditedAndDeletedSeedRowsWithoutDuplicates()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var factory = new CustomWebApplicationFactory();
        using var client = factory.CreateClient();

        using var firstResponse = await client.GetAsync("/api/domains", cancellationToken);
        firstResponse.EnsureSuccessStatusCode();

        string originalDomainDescription;
        string originalModuleTitle;
        string originalMaterialBody;
        string originalCompetencyName;
        string originalObjectiveText;
        Guid moduleId;
        Guid prerequisiteModuleId;

        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<PselDbContext>();

            var domain = await dbContext
                .Set<LearningDomain>()
                .SingleAsync(domain => domain.Slug == "programming-fundamentals", cancellationToken);
            originalDomainDescription = domain.Description;
            domain.Description = "Tampered domain description.";

            var module = await dbContext
                .Set<LearningModule>()
                .SingleAsync(module => module.Slug == "complexity-analysis", cancellationToken);
            originalModuleTitle = module.Title;
            module.Title = "Tampered module title";
            moduleId = module.Id;

            var prerequisiteModule = await dbContext
                .Set<LearningModule>()
                .SingleAsync(module => module.Slug == "algorithm-foundations", cancellationToken);
            prerequisiteModuleId = prerequisiteModule.Id;

            var material = await dbContext
                .Set<LearningMaterial>()
                .SingleAsync(
                    material => material.Slug == "complexity-analysis-lesson",
                    cancellationToken);
            originalMaterialBody = material.BodyMarkdown;
            material.BodyMarkdown = "Tampered material body.";

            var competency = await dbContext
                .Set<Competency>()
                .SingleAsync(competency => competency.Slug == "asymptotic-analysis", cancellationToken);
            originalCompetencyName = competency.Name;
            competency.Name = "Tampered competency name";

            var objective = await dbContext
                .Set<LearningObjective>()
                .SingleAsync(
                    objective => objective.ModuleId == moduleId && objective.SortOrder == 1,
                    cancellationToken);
            originalObjectiveText = objective.Text;
            objective.Text = "Tampered objective text.";

            dbContext.Remove(await dbContext
                .Set<LearningMaterial>()
                .SingleAsync(
                    material => material.Slug == "algorithm-foundations-lesson",
                    cancellationToken));
            dbContext.Remove(await dbContext
                .Set<LearningModulePrerequisite>()
                .SingleAsync(
                    prerequisite => prerequisite.ModuleId == moduleId
                        && prerequisite.PrerequisiteModuleId == prerequisiteModuleId,
                    cancellationToken));

            await dbContext.SaveChangesAsync(cancellationToken);
        }

        var seeder = factory.Services
            .GetServices<IHostedService>()
            .OfType<LearningDataSeeder>()
            .Single();
        await seeder.StartAsync(cancellationToken);

        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<PselDbContext>();

            Assert.Equal(
                originalDomainDescription,
                (await dbContext
                    .Set<LearningDomain>()
                    .SingleAsync(domain => domain.Slug == "programming-fundamentals", cancellationToken))
                .Description);

            var module = await dbContext
                .Set<LearningModule>()
                .SingleAsync(module => module.Slug == "complexity-analysis", cancellationToken);
            Assert.Equal(originalModuleTitle, module.Title);

            Assert.Equal(
                originalMaterialBody,
                (await dbContext
                    .Set<LearningMaterial>()
                    .SingleAsync(
                        material => material.Slug == "complexity-analysis-lesson",
                        cancellationToken))
                .BodyMarkdown);

            Assert.Equal(
                originalCompetencyName,
                (await dbContext
                    .Set<Competency>()
                    .SingleAsync(competency => competency.Slug == "asymptotic-analysis", cancellationToken))
                .Name);

            Assert.Equal(
                originalObjectiveText,
                (await dbContext
                    .Set<LearningObjective>()
                    .SingleAsync(
                        objective => objective.ModuleId == module.Id && objective.SortOrder == 1,
                        cancellationToken))
                .Text);

            Assert.True(await dbContext
                .Set<LearningMaterial>()
                .AnyAsync(
                    material => material.Slug == "algorithm-foundations-lesson",
                    cancellationToken));
            Assert.True(await dbContext
                .Set<LearningModulePrerequisite>()
                .AnyAsync(
                    prerequisite => prerequisite.ModuleId == module.Id
                        && prerequisite.PrerequisiteModuleId == prerequisiteModuleId,
                    cancellationToken));

            Assert.Equal(6, await dbContext.Set<LearningDomain>().CountAsync(cancellationToken));
            Assert.Equal(19, await dbContext.Set<LearningModule>().CountAsync(cancellationToken));
            Assert.Equal(19, await dbContext.Set<LearningMaterial>().CountAsync(cancellationToken));
            Assert.Equal(38, await dbContext.Set<Competency>().CountAsync(cancellationToken));
            Assert.Equal(57, await dbContext.Set<LearningObjective>().CountAsync(cancellationToken));
            Assert.Equal(
                13,
                await dbContext.Set<LearningModulePrerequisite>().CountAsync(cancellationToken));
        }
    }
}
