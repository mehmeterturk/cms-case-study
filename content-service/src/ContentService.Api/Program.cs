using ContentService.Api.Middleware;
using ContentService.Application;
using ContentService.Application.Common;
using ContentService.Infrastructure;
using ContentService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.Configure<LocalizationOptions>(builder.Configuration.GetSection(LocalizationOptions.SectionName));

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
