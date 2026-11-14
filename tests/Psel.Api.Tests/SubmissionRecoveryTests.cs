using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Psel.Infrastructure.Persistence;
using Psel.Modules.Submissions.Domain;
using Psel.Modules.Submissions.Services;

namespace Psel.Api.Tests;

public sealed class SubmissionRecoveryTests
{
    [Fact]
    public async Task StartAsync_MarksQueuedAndRunningSubmissionsInterrupted()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var factory = new CustomWebApplicationFactory();
        using var client = factory.CreateClient();

        Guid queuedId;
        Guid runningId;

        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<PselDbContext>();
            var queued = Submission.Create(
                "two-sum",
                "csharp",
                SubmissionPurpose.Run,
                "[]",
                DateTimeOffset.UtcNow);
            var running = Submission.Create(
                "two-sum",
                "csharp",
                SubmissionPurpose.Run,
                "[]",
                DateTimeOffset.UtcNow);
            running.MarkRunning(DateTimeOffset.UtcNow);

            dbContext.AddRange(queued, running);
            await dbContext.SaveChangesAsync(cancellationToken);

            queuedId = queued.Id;
            runningId = running.Id;
        }

        var recovery = new SubmissionRecoveryService(
            factory.Services.GetRequiredService<IServiceScopeFactory>(),
            NullLogger<SubmissionRecoveryService>.Instance);

        await recovery.StartAsync(cancellationToken);

        await using var verificationScope = factory.Services.CreateAsyncScope();
        var verification = verificationScope.ServiceProvider.GetRequiredService<PselDbContext>();
        var stored = await verification
            .Set<Submission>()
            .AsNoTracking()
            .Where(submission => submission.Id == queuedId || submission.Id == runningId)
            .ToListAsync(cancellationToken);

        Assert.Equal(2, stored.Count);
        Assert.All(
            stored,
            submission =>
            {
                Assert.Equal(SubmissionStatus.Interrupted, submission.Status);
                Assert.NotNull(submission.CompletedAtUtc);
            });
    }
}
