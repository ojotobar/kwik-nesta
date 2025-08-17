using CrossQueue.Hub.Shared.Extensions;
using DRY.MailJetClient.Library.Extensions;
using NotificationService.Workers;
using NotificationService.Workers.Handlers;
using NotificationService.Workers.Services;
using NotificationService.Workers.Services.Interfaces;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddHostedService<EmailNotificationsWorker>();
builder.Services.AddScoped<IMessageHandler, MessageHandler>();
builder.Services.AddScoped<IEmailSenders, EmailSender>();
builder.Services.ConfigureMailJet(builder.Configuration);
builder.Services.AddCrossQueueHubRabbitMqBus(builder.Configuration);
var host = builder.Build();
host.Run();
