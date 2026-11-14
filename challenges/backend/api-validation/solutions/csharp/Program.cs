var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

app.MapPost("/registrations", (RegistrationRequest request) =>
{
    var errors = new Dictionary<string, string[]>();

    if (string.IsNullOrWhiteSpace(request.Email)
        || request.Email.Length > 254
        || !request.Email.Contains('@', StringComparison.Ordinal))
    {
        errors["email"] = ["A valid email address is required."];
    }

    var displayName = request.DisplayName?.Trim() ?? string.Empty;

    if (displayName.Length is < 2 or > 50)
    {
        errors["displayName"] = ["Display name must be between 2 and 50 characters."];
    }

    if (request.Age is < 13 or > 120)
    {
        errors["age"] = ["Age must be between 13 and 120."];
    }

    if (errors.Count > 0)
    {
        return Results.ValidationProblem(errors);
    }

    return Results.Created($"/registrations/{Guid.NewGuid():N}", request);
});

app.Run();

public sealed record RegistrationRequest(string Email, string DisplayName, int Age);
