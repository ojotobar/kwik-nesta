using CrossQueue.Hub.Shared.Extensions;
using DiagnosKit.Core.Extensions;
using IdentityService.Api.Extensions;
using IdentityService.Infrastructure;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services
    .ConfigureIdentityAndDbContext(builder.Configuration)
    .ConfigureCors()
    .ConfigureServices()
    .ConfigureSwaggerDocs()
    .ConfigureApiVersioning()
    .AddCrossQueueHubRabbitMqBus(builder.Configuration)
    .ConfigureJwt(builder.Configuration)
    .AddLoggerManager();

builder.Services.AddAuthorization();

var app = builder.Build();

var logger = app.Services.GetRequiredService<ILogger<Program>>();
app.UseUnifiedErrorHandler();
await app.SeedInitialData(logger);

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthentication(); 
app.UseAuthorization();

app.MapControllers();
if (app.Environment.IsDevelopment())
{
    // Run migrations at startup (optional)
    using (var scope = app.Services.CreateScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.Database.Migrate();
    }
}
app.UseCors("CorsPolicy");
app.Run();
