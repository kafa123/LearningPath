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

var app = builder.Build();

app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapHealthEndpoints();
app.MapLearningEndpoints();
app.MapChallengeEndpoints();
app.MapExecutionEndpoints();
app.MapAssessmentEndpoints();
app.MapSubmissionEndpoints();
app.MapProgressEndpoints();

app.Run();

public partial class Program
{
}
