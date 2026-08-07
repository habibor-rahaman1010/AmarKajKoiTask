using AmarKajKoi.Extensions;
using AmarKajKoi.Hubs;
using AmarKajKoi.Middleware;
using Microsoft.OpenApi.Models;
using Serilog;
using Serilog.Events;

namespace AmarKajKoi
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var bootstrapConfig = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json")
                .Build();

            Log.Logger = new LoggerConfiguration()
                .ReadFrom.Configuration(bootstrapConfig)
                .CreateBootstrapLogger();

            try
            {
                Log.Information("Application Starting...");

                var builder = WebApplication.CreateBuilder(args);

                builder.Host.UseSerilog((ctx, lc) => lc
                    .MinimumLevel.Debug()
                    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
                    .Enrich.FromLogContext()
                    .ReadFrom.Configuration(builder.Configuration));

                builder.Services.AddAppServices(builder.Configuration);
                builder.Services.AddAppAuth(builder.Configuration);
                builder.Services.AddAppCors();

                builder.Services.AddControllers(options => options.Filters.Add<RealtimeNotificationFilter>());
                builder.Services.AddEndpointsApiExplorer();
                builder.Services.AddOpenApi(options =>
                {
                    options.AddDocumentTransformer((documents, context, cancellationToken) =>
                    {
                        documents.Components ??= new OpenApiComponents();
                        documents.Components.SecuritySchemes ??= new Dictionary<string, OpenApiSecurityScheme>();
                        documents.Components.SecuritySchemes["Bearer"] = new OpenApiSecurityScheme
                        {
                            Type = SecuritySchemeType.Http,
                            Scheme = "bearer",
                            BearerFormat = "JWT",
                            In = ParameterLocation.Header,
                            Description = "Token from POST /api/auth/login — paste the raw token, no \"Bearer \" prefix."
                        };
                        documents.SecurityRequirements.Add(new OpenApiSecurityRequirement
                        {
                            [new OpenApiSecurityScheme
                            {
                                Reference = new OpenApiReference
                                {
                                    Id = "Bearer",
                                    Type = ReferenceType.SecurityScheme
                                }
                            }] = Array.Empty<string>()
                        });
                        return Task.CompletedTask;
                    });
                });

                var app = builder.Build();

                if (app.Environment.IsDevelopment())
                {
                    app.MapOpenApi();
                    app.UseSwaggerUI(options =>
                    {
                        options.SwaggerEndpoint("/openapi/v1.json", "AmarKajKoi API v1");
                        options.RoutePrefix = "swagger";
                        options.DocumentTitle = "Amar Kaj Koi API";
                    });

                    app.MapGet("/", () => Results.Redirect("/swagger")).ExcludeFromDescription();
                }

                app.UseMiddleware<ErrorHandlingMiddleware>();
                app.UseCors("AppCors");
                app.UseAuthentication();
                app.UseAuthorization();
                app.MapControllers();
                app.MapHub<NotificationHub>("/hubs/notifications");

                await app.RunAsync();
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "Failed to start application!");
            }
            finally
            {
                await Log.CloseAndFlushAsync();
            }
        }
    }
}