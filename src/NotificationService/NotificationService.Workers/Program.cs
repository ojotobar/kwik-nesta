using CrossQueue.Hub.Shared.Extensions;
using DiagnosKit.Core.Configurations;
using DiagnosKit.Core.Extensions;
using DiagnosKit.Core.Logging;
using DRY.MailJetClient.Library.Extensions;
using EFCore.CrudKit.Library.Extensions;
using EFCore.CrudKit.Library.Models.Enums;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using NotificationService.Workers;
using NotificationService.Workers.Handlers;
using NotificationService.Workers.Services;
using NotificationService.Workers.Services.Interfaces;

SerilogBootstrapper.UseBootstrapLogger();
var builder = WebApplication.CreateBuilder(args);
builder.ConfigureSerilogESSink();
builder.Services.AddLoggerManager();
builder.Services.AddHostedService<EmailNotificationsWorker>();
builder.Services.AddHostedService<AuditLoggerWorker>();
builder.Services.AddScoped<IMessageHandler, MessageHandler>();
builder.Services.AddScoped<IEmailSenders, EmailSender>();
builder.Services.ConfigureMailJet(builder.Configuration);
builder.Services.AddCrossQueueHubRabbitMqBus(builder.Configuration);
builder.Services.ConfigureMongoEFCoreDataForge(builder.Configuration, idSerializationMode: IdSerializationMode.Guid);
builder.Services.ConfigureDataForgeRawCrudKit(builder.Configuration);
builder.Services.AddScoped<IAuditService, AuditService>();

var host = builder.Build();
host.MapGet("/favicon.ico", () => Results.NoContent());
host.MapGet("/audits", async (IAuditService service) =>
{
    var audits = await service.GetAuditTrails();
    return Results.Ok(audits);
});
host.Run();
