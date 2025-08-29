using DiagnosKit.Core.Configurations;
using DiagnosKit.Core.Logging;
using Microsoft.AspNetCore.Builder;
using NotificationService.Workers.Helpers;

SerilogBootstrapper.UseBootstrapLogger();
var builder = WebApplication.CreateBuilder(args);
builder.ConfigureSerilogESSink();
builder.Services.RegisterServices(builder.Configuration,
                                  builder.Environment.ApplicationName);

var host = builder.Build();
host.UseAuthentication();
host.UseAuthorization();
host.CallEndPoints();
host.Run();
