using Microsoft.AspNetCore.Http.Features;
using Psel.Api.Endpoints;
using Psel.Infrastructure;
using Psel.Modules.Assessment;
using Psel.Modules.Challenges;
using Psel.Modules.Execution;
using Psel.Modules.Learning;
using Psel.Modules.Progress;
using Psel.Modules.Submissions;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddInfrastructure(
    builder.Configuration,
    builder.Environment,
    LearningModuleRegistration.ModelAssembly,
    SubmissionModuleRegistration.ModelAssembly,
    AssessmentModuleRegistration.ModelAssembly,
    ProgressModuleRegistration.ModelAssembly);
builder.Services.AddExecutionModule(builder.Configuration);
builder.Services.AddLearningModule();
builder.Services.AddChallengesModule();
builder.Services.AddAssessmentModule();
builder.Services.AddSubmissionsModule();
builder.Services.AddProgressModule();
builder.Services.AddProblemDetails();
builder.Services.AddOpenApi();
builder.Services.Configure<FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = 25 * 1024 * 1024;
    options.ValueCountLimit = 64;
});

var app = builder.Build();

app.UseExceptionHandler();
app.Use(async (context, next) =>
{
    if (context.Request.Path.StartsWithSegments("/api/authoring"))
    {
        var feature = context.Features.Get<IHttpMaxRequestBodySizeFeature>();

        if (feature is { IsReadOnly: false })
        {
            feature.MaxRequestBodySize = context.Request.Path.StartsWithSegments(
                "/api/authoring/challenge-packages")
                ? 25 * 1024 * 1024
                : 2 * 1024 * 1024;
        }
    }

    await next(context);
});

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapHealthEndpoints();
app.MapLearningEndpoints();
app.MapLearningAuthoringEndpoints();
app.MapChallengeEndpoints();
app.MapChallengeAuthoringEndpoints();
app.MapExecutionEndpoints();
app.MapAssessmentEndpoints();
app.MapSubmissionEndpoints();
app.MapProgressEndpoints();

app.Run();

public partial class Program
{
}
