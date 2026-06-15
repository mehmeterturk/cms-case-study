using Microsoft.EntityFrameworkCore;
using UserService.Api.Middleware;
using UserService.Application;
using UserService.Infrastructure;
using UserService.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddHealthChecks();

var app = builder.Build();

// Veritabanı şemasını uygulama açılışında migrate et (Docker ortamında otomatik kurulum için).
await using (var scope = app.Services.CreateAsyncScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.MigrateAsync();
}

app.UseMiddleware<ExceptionHandlingMiddleware>();

app.UseSwagger();
app.UseSwaggerUI();

app.MapControllers();
app.MapHealthChecks("/health");

app.Run();

// Integration testlerinin WebApplicationFactory ile erişebilmesi için.
public partial class Program;
