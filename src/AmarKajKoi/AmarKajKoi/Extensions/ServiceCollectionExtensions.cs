using AmarKajKoi.Database;
using AmarKajKoi.Services;
using AmarKajKoi.ServicesImplement;
using AmarKajKoi.ServicesInterface;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace AmarKajKoi.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddAppServices(this IServiceCollection services, IConfiguration config)
        {
            var connectionString = config.GetConnectionString("Default")
                                   ?? throw new InvalidOperationException("ConnectionStrings:Default missing");

            services.AddSingleton<IDbConnectionFactory>(new DbConnectionFactory(connectionString));
            services.AddScoped<IUnitOfWork, UnitOfWork>();

            services.AddSingleton<IPasswordHasher, PasswordHasher>();
            services.AddSingleton<IJwtTokenService, JwtTokenService>();

            services.AddScoped<IAuthService, AuthService>();
            services.AddScoped<ITaskService, TaskService>();
            services.AddScoped<IVoiceService, VoiceService>();
            services.AddScoped<INotificationService, NotificationService>();
            services.AddScoped<IReferenceDataService, ReferenceDataService>();

            // Scoped: it buffers the recipients of one request/operation, then flushes.
            services.AddScoped<IRealtimeNotifier, RealtimeNotifier>();
            services.AddSignalR();

            // Background worker for auto-Overdue, escalations, and stale voice review reminders
            services.AddHostedService<TaskAutomationHostedService>();

            return services;
        }

        public static IServiceCollection AddAppAuth(this IServiceCollection services, IConfiguration config)
        {
            var section = config.GetSection("Jwt");
            var secret = section["Secret"] ?? throw new InvalidOperationException("Jwt:Secret missing");
            var issuer = section["Issuer"] ?? "AmarKajKoi";
            var audience = section["Audience"] ?? "AmarKajKoiUsers";

            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                    .AddJwtBearer(o =>
                    {
                        o.TokenValidationParameters = new TokenValidationParameters
                        {
                            ValidateIssuer = true,
                            ValidateAudience = true,
                            ValidateLifetime = true,
                            ValidateIssuerSigningKey = true,
                            ValidIssuer = issuer,
                            ValidAudience = audience,
                            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret))
                        };

                        // A browser cannot attach an Authorization header to a WebSocket
                        // handshake, so the SignalR client passes the token as a query
                        // string instead. Accepted for hub paths only.
                        o.Events = new JwtBearerEvents
                        {
                            OnMessageReceived = ctx =>
                            {
                                var accessToken = ctx.Request.Query["access_token"];
                                if (!string.IsNullOrEmpty(accessToken) &&
                                    ctx.HttpContext.Request.Path.StartsWithSegments("/hubs"))
                                {
                                    ctx.Token = accessToken;
                                }
                                return Task.CompletedTask;
                            }
                        };
                    });

            services.AddAuthorization();
            return services;
        }

        public static IServiceCollection AddAppCors(this IServiceCollection services)
        {
            services.AddCors(o => o.AddPolicy("AppCors", p => p
                .WithOrigins("http://localhost:4200", "http://localhost:4300")
                .AllowAnyHeader()
                .AllowAnyMethod()
                .AllowCredentials()));
            return services;
        }
    }
}
