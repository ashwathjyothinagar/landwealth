var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "LandWealth API",
        Version = "v1",
        Description = "Personal Wealth and Land/Property Investment Management API"
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "LandWealth API v1");
    });
}

app.UseHttpsRedirection();

app.UseRouting();

// Health check endpoint
app.MapGet("/api/health", () => Results.Ok(new
{
    status = "Healthy",
    timestamp = DateTime.UtcNow,
    application = "LandWealth.Api"
}));

app.MapControllers();

app.Run();

// Required for WebApplicationFactory<Program> in IntegrationTests
public partial class Program { }
