var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

app.MapPost("/registrations", (RegistrationRequest request) =>
{
    // TODO: validate email, displayName, and age before creating anything.
    return Results.Created($"/registrations/{Guid.NewGuid():N}", request);
});

app.Run();

public sealed record RegistrationRequest(string Email, string DisplayName, int Age);
