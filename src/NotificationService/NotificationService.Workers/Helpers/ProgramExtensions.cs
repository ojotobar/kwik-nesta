using CrossQueue.Hub.Shared.Extensions;
using CSharpTypes.Extensions.Enumeration;
using DiagnosKit.Core.Extensions;
using DRY.MailJetClient.Library.Extensions;
using EFCore.CrudKit.Library.Extensions;
using EFCore.CrudKit.Library.Models.Enums;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.IdentityModel.Tokens;
using NotificationService.Workers.Handlers;
using NotificationService.Workers.Models;
using NotificationService.Workers.Models.Enums;
using NotificationService.Workers.Services;
using NotificationService.Workers.Services.Interfaces;
using System.Security.Claims;
using System.Text;

namespace NotificationService.Workers.Helpers
{
    public static class ProgramExtensions
    {
        public static IServiceCollection RegisterServices(this IServiceCollection services,
                                                          IConfiguration configuration,
                                                          string serviceName)
        {
            var jwtSection = configuration.GetSection("Jwt");
            services.ConfigureMongoEFCoreDataForge(configuration, idSerializationMode: IdSerializationMode.Guid);
            services.AddLoggerManager();
            services.AddHostedService<EmailNotificationsWorker>()
                .AddHostedService<AuditLoggerWorker>()
                .AddScoped<IMessageHandler, MessageHandler>()
                .AddScoped<IEmailSenders, EmailSender>()
                .ConfigureMailJet(configuration)
                .AddCrossQueueHubRabbitMqBus(configuration)
                .ConfigureDataForgeRawCrudKit(configuration)
                .AddScoped<IAuditService, AuditService>()
                .AddDiagnosKitObservability(serviceName: serviceName, serviceVersion: "1.0.0")
                .AddAuthentication("Bearer")
                .AddJwtBearer("Bearer", options =>
                {
                    options.TokenValidationParameters = new()
                    {
                        ValidateIssuer = true,
                        ValidIssuer = jwtSection.GetValue<string>("IdentityService"),

                        ValidateLifetime = true,

                        ValidateAudience = true,
                        ValidAudiences = new[] { jwtSection.GetValue<string>("Audience") },

                        RoleClaimType = jwtSection.GetValue<string>("RoleClaim"),

                        IssuerSigningKey = new SymmetricSecurityKey(
                            Encoding.UTF8.GetBytes(jwtSection.GetValue<string>("SigningKey")!))
                    };

                    options.Events = new JwtBearerEvents
                    {
                        OnTokenValidated = async context =>
                        {
                            var service = context.HttpContext.RequestServices.GetRequiredService<IAuditService>();
                            var userId = context.Principal?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                            if (string.IsNullOrEmpty(userId))
                            {
                                context.Fail("Forbidden: Invalid identifier");
                                return;
                            }

                            var user = await service.GetUserById(userId);
                            if (user is null)
                            {
                                context.Fail("Forbidden: User not found");
                                return;
                            }

                            if (user.Status != AspNetUserStatus.Active)
                            {
                                context.Fail($"Forbidden: Your account has been {user.Status.GetDescription()}");
                            }
                        }
                    };
                });

                services.AddAuthorization(options =>
                {
                    options.AddPolicy("AdminOnly", policy =>
                        policy.RequireRole("Admin", "SuperAdmin"));
                });

            return services;
        }

        public static WebApplication CallEndPoints(this WebApplication app)
        {
            app.MapGet("/favicon.ico", () => Results.NoContent());
            app.MapGet("/audits", 
                [Authorize(Policy = "AdminOnly")]
                async (IAuditService service, [AsParameters] AuditQuery query) =>
                {
                    var audits = await service.GetAuditTrails(query);
                    return Results.Ok(audits);
                });

            return app;
        }
    }
}
