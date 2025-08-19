using CrossQueue.Hub.Services.Interfaces;
using CSharpTypes.Extensions.Enumeration;
using NotificationService.Workers.Handlers;
using NotificationService.Workers.Models;
using NotificationService.Workers.Models.Enums;

namespace NotificationService.Workers
{
    public class EmailNotificationsWorker : BackgroundService
    {
        private readonly IServiceScopeFactory scopeFactory;
        private readonly ILogger<EmailNotificationsWorker> _logger;
        private readonly IRabbitMQPubSub _pubSub;
        private readonly IMessageHandler _handler;

        public EmailNotificationsWorker(IServiceScopeFactory scopeFactory, 
            ILogger<EmailNotificationsWorker> logger, IRabbitMQPubSub pubSub)
        {
            using var scope = scopeFactory.CreateScope();
            var handler = scope.ServiceProvider.GetRequiredService<IMessageHandler>();
            this.scopeFactory = scopeFactory;
            _logger = logger;
            _pubSub = pubSub;
            _handler = handler;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Worker started....");

            _pubSub.Subscribe<EmailNotification>(RabbitMqQueues.Notification.GetDescription(), async msg =>
            {
                _logger.LogInformation("Received email notification for {Email}", msg.EmailAddress);
                await _handler.HandleAsync(msg);
            }, routingKey: RabbitMqRoutingKey.AccountEmail.GetDescription());

            await Task.CompletedTask;
        }
    }
}
