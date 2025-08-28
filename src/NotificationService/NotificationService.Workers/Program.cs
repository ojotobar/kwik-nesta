using CrossQueue.Hub.Shared.Extensions;
using CSharpTypes.Extensions.Enumeration;
using DiagnosKit.Core.Configurations;
using DiagnosKit.Core.Extensions;
using DiagnosKit.Core.Logging;
using DRY.MailJetClient.Library.Extensions;
using EFCore.CrudKit.Library.Extensions;
using EFCore.CrudKit.Library.Models.Enums;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.IdentityModel.Tokens;
using NotificationService.Workers;
using NotificationService.Workers.Handlers;
using NotificationService.Workers.Models;
using NotificationService.Workers.Models.Enums;
using NotificationService.Workers.Services;
using NotificationService.Workers.Services.Interfaces;
using System.Security.Claims;
using System.Text;

SerilogBootstrapper.UseBootstrapLogger();
var builder = WebApplication.CreateBuilder(args);
var jwtSection = builder.Configuration.GetSection("Jwt");
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
builder.Services
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

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy =>
        policy.RequireRole("Admin", "SuperAdmin"));
});

var host = builder.Build();
host.UseAuthentication();
host.UseAuthorization();
host.MapGet("/favicon.ico", () => Results.NoContent());

host.MapGet("/audits", 
    [Authorize(Policy = "AdminOnly")] 
    async (IAuditService service, [AsParameters] AuditQuery query) =>
{
    var audits = await service.GetAuditTrails(query);
    return Results.Ok(audits);
});
host.Run();
