using CrossQueue.Hub.Shared.Extensions;
using DiagnosKit.Core.Configurations;
using DiagnosKit.Core.Extensions;
using DiagnosKit.Core.Logging;
using DRY.MailJetClient.Library.Extensions;
using NotificationService.Workers;
using NotificationService.Workers.Handlers;
using NotificationService.Workers.Services;
using NotificationService.Workers.Services.Interfaces;

SerilogBootstrapper.UseBootstrapLogger();
var builder = Host.CreateApplicationBuilder(args);
builder.ConfigureSerilogESSink();
builder.Services.AddLoggerManager();
builder.Services.AddHostedService<EmailNotificationsWorker>();
builder.Services.AddScoped<IMessageHandler, MessageHandler>();
builder.Services.AddScoped<IEmailSenders, EmailSender>();
builder.Services.ConfigureMailJet(builder.Configuration);
builder.Services.AddCrossQueueHubRabbitMqBus(builder.Configuration);

var host = builder.Build();
host.Run();
