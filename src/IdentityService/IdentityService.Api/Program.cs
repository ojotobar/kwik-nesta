using DiagnosKit.Core.Extensions;
using IdentityService.Api.Extensions;
using IdentityService.Domain.Entities;
using IdentityService.Infrastructure;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.ConfigureIdentityAndDbContext(builder.Configuration)
    .ConfigureRSAEncryption(builder.Configuration)
    .ConfigureCors()
    .ConfigureServices()
    .ConfigureSwaggerDocs()
    .ConfigureApiVersioning()
    .AddLoggerManager();

builder.Services.Configure<Jwt>(builder.Configuration.GetSection("Jwt"));
var app = builder.Build();
app.UseUnifiedErrorHandler();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

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
