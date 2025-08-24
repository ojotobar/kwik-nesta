using CrossQueue.Hub.Services.Interfaces;
using CSharpTypes.Extensions.Enumeration;
using DiagnosKit.Core.Logging.Contracts;
using KwikNesta.Contracts.Enums;
using KwikNesta.Contracts.Models;
using NotificationService.Workers.Handlers;

namespace NotificationService.Workers
{
    public class AuditLoggerWorker : BackgroundService
    {
        private readonly IRabbitMQPubSub _pubSub;
        private readonly ILoggerManager _logger;
        private readonly IMessageHandler _handler;

        public AuditLoggerWorker(IServiceScopeFactory scopeFactory, 
            IRabbitMQPubSub pubSub, ILoggerManager logger)
        {
            using var scope = scopeFactory.CreateScope();
            var handler = scope.ServiceProvider.GetRequiredService<IMessageHandler>();
            _pubSub = pubSub;
            _logger = logger;
            _handler = handler;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInfo("Audit Trail Worker started....");

            _pubSub.Subscribe<AuditLog>(MQs.Audit.GetDescription(), async msg =>
            {
                _logger.LogInfo("Received audit trail for action: {Action}", msg.Action);
                await _handler.HandleAsync(msg);
            }, routingKey: MQRoutingKey.AuditTrails.GetDescription());

            await Task.CompletedTask;
        }
    }
}
