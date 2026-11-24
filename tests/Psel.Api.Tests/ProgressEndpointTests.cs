using System.Net.Http.Json;

namespace Psel.Api.Tests;

public sealed class ProgressEndpointTests
{
    [Fact]
    public async Task GetProgress_Initially_ReportsEveryCompetencyNotStarted()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var factory = new CustomWebApplicationFactory();
        using var client = factory.CreateClient();

        var overview = await client.GetFromJsonAsync<ProgressOverviewDto>(
            "/api/progress",
            Phase7TestSupport.JsonOptions,
            cancellationToken);

        Assert.NotNull(overview);
        var competencies = overview.Domains
            .SelectMany(domain => domain.Modules)
            .SelectMany(module => module.Competencies)
            .ToList();

        Assert.NotEmpty(competencies);
        Assert.All(competencies, competency => Assert.Equal("NotStarted", competency.State));
        Assert.All(competencies, competency => Assert.Empty(competency.Evidence));
        Assert.Equal(competencies.Count, overview.Summary.Total);
        Assert.Equal(competencies.Count, overview.Summary.NotStarted);
        Assert.Equal(0, overview.Summary.Learning);
        Assert.Equal(0, overview.Summary.Practicing);
        Assert.Equal(0, overview.Summary.AssessmentRequired);
        Assert.Equal(0, overview.Summary.Mastered);

        Assert.Equal(6, overview.Domains.Count);
        Assert.All(
            overview.Domains,
            domain => Assert.All(domain.Modules, module => Assert.NotEmpty(module.Competencies)));
    }

    [Fact]
    public async Task GetProgress_AfterRunOnly_ReportsLearning()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var factory = new CustomWebApplicationFactory();
        factory.Runner.Handler = request =>
            AssessmentTestOutput.Success(AssessmentTestOutput.Build(request));
        using var client = factory.CreateClient();

        var created = await Phase7TestSupport.PostSubmissionAsync(
            client,
            "two-sum",
            "Run",
            null,
            cancellationToken);
        await Phase7TestSupport.WaitForTerminalAsync(client, created.Id, cancellationToken);

        var overview = await GetOverviewAsync(client, cancellationToken);
        var competency = Phase7TestSupport.FindCompetency(overview, "algorithm-decomposition");

        Assert.Equal("Learning", competency.State);
        var evidence = Assert.Single(competency.Evidence);
        Assert.Equal("Run", evidence.Purpose);
        Assert.Equal("two-sum", evidence.ChallengeSlug);
        Assert.False(evidence.IsExamination);
        AssertSummaryConsistent(overview);
    }

    [Fact]
    public async Task GetProgress_AfterFailedSubmit_ReportsPracticing()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var factory = new CustomWebApplicationFactory();
        factory.Runner.Handler = request =>
            AssessmentTestOutput.Success(AssessmentTestOutput.Build(
                request,
                test => test.Id == "hidden-1" ? (false, false) : (true, false)));
        using var client = factory.CreateClient();

        var created = await Phase7TestSupport.PostSubmissionAsync(
            client,
            "two-sum",
            "Submit",
            null,
            cancellationToken);
        await Phase7TestSupport.WaitForTerminalAsync(client, created.Id, cancellationToken);

        var overview = await GetOverviewAsync(client, cancellationToken);
        var competency = Phase7TestSupport.FindCompetency(overview, "algorithm-decomposition");

        Assert.Equal("Practicing", competency.State);
        var evidence = Assert.Single(competency.Evidence);
        Assert.Equal("WrongAnswer", evidence.Outcome);
        Assert.False(evidence.IsExamination);
        AssertSummaryConsistent(overview);
    }

    [Fact]
    public async Task GetProgress_AfterAcceptedSubmitWithoutExamination_ReportsAssessmentRequired()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var factory = new CustomWebApplicationFactory();
        factory.Runner.Handler = request =>
            AssessmentTestOutput.Success(AssessmentTestOutput.Build(request));
        using var client = factory.CreateClient();

        var created = await Phase7TestSupport.PostSubmissionAsync(
            client,
            "two-sum",
            "Submit",
            null,
            cancellationToken);
        await Phase7TestSupport.WaitForTerminalAsync(client, created.Id, cancellationToken);

        var overview = await GetOverviewAsync(client, cancellationToken);
        var competency = Phase7TestSupport.FindCompetency(overview, "algorithm-decomposition");

        Assert.Equal("AssessmentRequired", competency.State);
        AssertSummaryConsistent(overview);
    }

    [Fact]
    public async Task GetProgress_AfterExaminationAndSecondChallenge_ReportsMastered()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var factory = new CustomWebApplicationFactory();
        factory.Runner.Handler = request =>
            AssessmentTestOutput.Success(AssessmentTestOutput.Build(request));
        using var client = factory.CreateClient();

        var examination = await Phase7TestSupport.CreateExaminationAsync(
            client,
            "two-sum",
            cancellationToken);
        var examinationAttempt = await Phase7TestSupport.PostSubmissionAsync(
            client,
            "two-sum",
            "Submit",
            examination.Id,
            cancellationToken);
        await Phase7TestSupport.WaitForTerminalAsync(
            client,
            examinationAttempt.Id,
            cancellationToken);

        var secondChallenge = await Phase7TestSupport.PostSubmissionAsync(
            client,
            "balanced-brackets",
            "Submit",
            null,
            cancellationToken);
        await Phase7TestSupport.WaitForTerminalAsync(
            client,
            secondChallenge.Id,
            cancellationToken);

        var overview = await GetOverviewAsync(client, cancellationToken);
        var competency = Phase7TestSupport.FindCompetency(overview, "algorithm-decomposition");

        Assert.Equal("Mastered", competency.State);
        AssertSummaryConsistent(overview);
        Assert.Contains(competency.Evidence, evidence => evidence.IsExamination);
        Assert.Contains(
            competency.Evidence,
            evidence => !evidence.IsExamination && evidence.ChallengeSlug == "balanced-brackets");
    }

    [Fact]
    public async Task GetAssessments_ReturnsNewestFirstAndFlagsExaminationAttempts()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var factory = new CustomWebApplicationFactory();
        factory.Runner.Handler = request =>
            AssessmentTestOutput.Success(AssessmentTestOutput.Build(request));
        using var client = factory.CreateClient();

        var practice = await Phase7TestSupport.PostSubmissionAsync(
            client,
            "two-sum",
            "Submit",
            null,
            cancellationToken);
        await Phase7TestSupport.WaitForTerminalAsync(client, practice.Id, cancellationToken);
        await Task.Delay(20, cancellationToken);

        var examination = await Phase7TestSupport.CreateExaminationAsync(
            client,
            "two-sum",
            cancellationToken);
        var examinationAttempt = await Phase7TestSupport.PostSubmissionAsync(
            client,
            "two-sum",
            "Submit",
            examination.Id,
            cancellationToken);
        await Phase7TestSupport.WaitForTerminalAsync(
            client,
            examinationAttempt.Id,
            cancellationToken);

        var history = await client.GetFromJsonAsync<List<AssessmentHistoryItemDto>>(
            "/api/assessments",
            Phase7TestSupport.JsonOptions,
            cancellationToken);

        Assert.NotNull(history);
        Assert.Equal(2, history.Count);

        var newest = history[0];
        Assert.Equal(examinationAttempt.Id, newest.SubmissionId);
        Assert.True(newest.IsExamination);
        Assert.Equal(examination.Id, newest.ExaminationId);
        Assert.NotNull(newest.ExaminationVariant);
        Assert.True(newest.IsAccepted);
        Assert.Equal("Accepted", newest.Outcome);

        var oldest = history[1];
        Assert.Equal(practice.Id, oldest.SubmissionId);
        Assert.False(oldest.IsExamination);
        Assert.Null(oldest.ExaminationId);
    }

    private static async Task<ProgressOverviewDto> GetOverviewAsync(
        HttpClient client,
        CancellationToken cancellationToken)
    {
        var overview = await client.GetFromJsonAsync<ProgressOverviewDto>(
            "/api/progress",
            Phase7TestSupport.JsonOptions,
            cancellationToken);

        Assert.NotNull(overview);
        return overview;
    }

    private static void AssertSummaryConsistent(ProgressOverviewDto overview)
    {
        var competencies = overview.Domains
            .SelectMany(domain => domain.Modules)
            .SelectMany(module => module.Competencies)
            .ToList();

        Assert.Equal(competencies.Count, overview.Summary.Total);
        Assert.Equal(
            competencies.Count(competency => competency.State == "NotStarted"),
            overview.Summary.NotStarted);
        Assert.Equal(
            competencies.Count(competency => competency.State == "Learning"),
            overview.Summary.Learning);
        Assert.Equal(
            competencies.Count(competency => competency.State == "Practicing"),
            overview.Summary.Practicing);
        Assert.Equal(
            competencies.Count(competency => competency.State == "AssessmentRequired"),
            overview.Summary.AssessmentRequired);
        Assert.Equal(
            competencies.Count(competency => competency.State == "Mastered"),
            overview.Summary.Mastered);
    }
}
