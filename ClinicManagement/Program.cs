using System.Security.Claims;
using ClinicManagement.Data;
using ClinicManagement.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using MySqlConnector;

namespace ClinicManagement
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);
            var port = builder.Configuration["PORT"];
            if (!string.IsNullOrWhiteSpace(port))
            {
                builder.WebHost.UseUrls($"http://0.0.0.0:{port}");
            }

            builder.Services.AddAuthentication("ClinicCookie")
                .AddCookie("ClinicCookie", options =>
                {
                    options.LoginPath = "/Auth/Login";
                    options.AccessDeniedPath = "/Auth/Denied";
                    options.Events = new CookieAuthenticationEvents
                    {
                        OnValidatePrincipal = ValidatePrincipalAsync
                    };
                });

            builder.Services.Configure<ForwardedHeadersOptions>(options =>
            {
                options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
                options.KnownNetworks.Clear();
                options.KnownProxies.Clear();
            });

            var connectionString = BuildMySqlConnectionString(builder.Configuration, builder.Environment);

            builder.Services.AddDbContext<ClinicDbContext>(options =>
                options.UseMySql(
                    connectionString,
                    new MySqlServerVersion(new Version(8, 0, 36)),
                    mysqlOptions => mysqlOptions.EnableRetryOnFailure(
                        maxRetryCount: 5,
                        maxRetryDelay: TimeSpan.FromSeconds(10),
                        errorNumbersToAdd: null)));

            builder.Services.AddSingleton<PasswordHashService>();
            builder.Services.AddSingleton<UserManualService>();
            builder.Services.AddMemoryCache();
            builder.Services.AddScoped<ClinicStore>();
            builder.Services.AddScoped<AiSchedulingService>();
            builder.Services.AddControllersWithViews();

            var app = builder.Build();
            ClinicDbInitializer.Initialize(app.Services);

            app.UseForwardedHeaders();

            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            app.UseAuthentication();
            app.Use(async (context, next) =>
            {
                var mustChangePassword = bool.TryParse(
                    context.User.FindFirstValue("MustChangePassword"),
                    out var required) &&
                    required;

                if (context.User.Identity?.IsAuthenticated == true &&
                    mustChangePassword &&
                    !context.Request.Path.StartsWithSegments("/Auth/ChangePassword") &&
                    !context.Request.Path.StartsWithSegments("/Auth/Logout") &&
                    !context.Request.Path.StartsWithSegments("/Auth/Denied"))
                {
                    context.Response.Redirect("/Auth/ChangePassword?required=1");
                    return;
                }

                await next();
            });
            app.UseStatusCodePages(context =>
            {
                var httpContext = context.HttpContext;
                if (httpContext.Response.StatusCode != StatusCodes.Status404NotFound ||
                    httpContext.Response.HasStarted ||
                    !IsPageNavigationRequest(httpContext))
                {
                    return Task.CompletedTask;
                }

                var redirectUrl = httpContext.User.Identity?.IsAuthenticated == true
                    ? "/"
                    : "/Auth/Login";
                httpContext.Response.Redirect(redirectUrl);
                return Task.CompletedTask;
            });
            app.UseAuthorization();

            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}");

            app.Run();
        }

        private static async Task ValidatePrincipalAsync(CookieValidatePrincipalContext context)
        {
            var userIdValue = context.Principal?.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdValue, out var userId))
            {
                await RejectPrincipalAsync(context);
                return;
            }

            var store = context.HttpContext.RequestServices.GetRequiredService<ClinicStore>();
            var user = store.GetUserForSession(userId);
            var roleClaim = context.Principal?.FindFirstValue(ClaimTypes.Role);
            var doctorClaim = context.Principal?.FindFirst("DoctorId")?.Value;
            var doctorId = int.TryParse(doctorClaim, out var parsedDoctorId) ? parsedDoctorId : (int?)null;
            var mustChangePasswordClaim = context.Principal?.FindFirst("MustChangePassword")?.Value;
            var mustChangePassword = bool.TryParse(mustChangePasswordClaim, out var parsedRequired) && parsedRequired;

            if (user is null ||
                !user.IsActive ||
                !string.Equals(user.Username, context.Principal?.Identity?.Name, StringComparison.OrdinalIgnoreCase) ||
                !string.Equals(user.Role.ToString(), roleClaim, StringComparison.Ordinal) ||
                user.DoctorId != doctorId ||
                user.MustChangePassword != mustChangePassword)
            {
                await RejectPrincipalAsync(context);
            }
        }

        private static async Task RejectPrincipalAsync(CookieValidatePrincipalContext context)
        {
            context.RejectPrincipal();
            await context.HttpContext.SignOutAsync("ClinicCookie");
        }

        private static bool IsPageNavigationRequest(HttpContext context)
        {
            if (!HttpMethods.IsGet(context.Request.Method) &&
                !HttpMethods.IsHead(context.Request.Method))
            {
                return false;
            }

            var path = context.Request.Path.Value;
            if (!string.IsNullOrWhiteSpace(path) &&
                !string.IsNullOrWhiteSpace(Path.GetExtension(path)))
            {
                return false;
            }

            var accept = context.Request.Headers.Accept.ToString();
            return string.IsNullOrWhiteSpace(accept) ||
                   accept.Contains("text/html", StringComparison.OrdinalIgnoreCase) ||
                   accept.Contains("*/*", StringComparison.OrdinalIgnoreCase);
        }

        private static string BuildMySqlConnectionString(IConfiguration configuration, IWebHostEnvironment environment)
        {
            var railwayUrl = FirstConfiguredValue(
                configuration,
                "MYSQL_URL",
                "DATABASE_URL",
                "MYSQL_PRIVATE_URL",
                "MYSQL_PUBLIC_URL");
            railwayUrl = NormalizeEnvironmentValue(railwayUrl);

            if (!string.IsNullOrWhiteSpace(railwayUrl) &&
                Uri.TryCreate(railwayUrl, UriKind.Absolute, out var mysqlUri))
            {
                var userInfo = mysqlUri.UserInfo.Split(':', 2);
                return BuildMySqlConnectionString(
                    mysqlUri.Host,
                    mysqlUri.Port > 0 ? mysqlUri.Port.ToString() : "3306",
                    Uri.UnescapeDataString(userInfo.ElementAtOrDefault(0) ?? string.Empty),
                    Uri.UnescapeDataString(userInfo.ElementAtOrDefault(1) ?? string.Empty),
                    mysqlUri.AbsolutePath.Trim('/'));
            }

            var mysqlHost = NormalizeEnvironmentValue(configuration["MYSQLHOST"]);
            if (!string.IsNullOrWhiteSpace(mysqlHost))
            {
                return BuildMySqlConnectionString(
                    mysqlHost,
                    NormalizeEnvironmentValue(configuration["MYSQLPORT"]) ?? "3306",
                    NormalizeEnvironmentValue(configuration["MYSQLUSER"]) ?? string.Empty,
                    NormalizeEnvironmentValue(configuration["MYSQLPASSWORD"]) ?? string.Empty,
                    NormalizeEnvironmentValue(configuration["MYSQLDATABASE"]) ?? string.Empty);
            }

            if (IsHostedProduction(configuration, environment))
            {
                throw new InvalidOperationException(
                    "Missing Railway MySQL configuration. In the web service Variables tab, add MYSQLHOST, MYSQLPORT, MYSQLUSER, MYSQLPASSWORD and MYSQLDATABASE from the MySQL service, or add MYSQL_URL. " +
                    BuildEnvironmentDiagnostics(configuration));
            }

            return configuration.GetConnectionString("DefaultConnection")
                   ?? throw new InvalidOperationException("Missing MySQL connection configuration.");
        }

        private static string? FirstConfiguredValue(IConfiguration configuration, params string[] keys)
        {
            return keys
                .Select(key => configuration[key])
                .FirstOrDefault(value => !string.IsNullOrWhiteSpace(value));
        }

        private static string? NormalizeEnvironmentValue(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return value;
            }

            return value.Trim().Trim('"');
        }

        private static string BuildEnvironmentDiagnostics(IConfiguration configuration)
        {
            var keys = new[]
            {
                "ASPNETCORE_ENVIRONMENT",
                "RAILWAY_ENVIRONMENT",
                "RAILWAY_PROJECT_ID",
                "RAILWAY_SERVICE_ID",
                "MYSQL_URL",
                "DATABASE_URL",
                "MYSQL_PRIVATE_URL",
                "MYSQL_PUBLIC_URL",
                "MYSQLHOST",
                "MYSQLPORT",
                "MYSQLUSER",
                "MYSQLPASSWORD",
                "MYSQLDATABASE"
            };

            var states = keys.Select(key =>
            {
                var value = configuration[key];
                var state = value is null
                    ? "missing"
                    : string.IsNullOrWhiteSpace(value)
                        ? "empty"
                        : $"present(len={value.Length})";

                return $"{key}={state}";
            });

            return $"Detected variables: {string.Join(", ", states)}.";
        }

        private static bool IsHostedProduction(IConfiguration configuration, IWebHostEnvironment environment)
        {
            return environment.IsProduction() ||
                   !string.IsNullOrWhiteSpace(configuration["RAILWAY_ENVIRONMENT"]) ||
                   !string.IsNullOrWhiteSpace(configuration["RAILWAY_PROJECT_ID"]) ||
                   !string.IsNullOrWhiteSpace(configuration["RAILWAY_SERVICE_ID"]);
        }

        private static string BuildMySqlConnectionString(
            string host,
            string port,
            string user,
            string password,
            string database)
        {
            host = NormalizeEnvironmentValue(host) ?? string.Empty;
            port = NormalizeEnvironmentValue(port) ?? string.Empty;
            user = NormalizeEnvironmentValue(user) ?? string.Empty;
            password = NormalizeEnvironmentValue(password) ?? string.Empty;
            database = NormalizeEnvironmentValue(database) ?? string.Empty;

            if (string.IsNullOrWhiteSpace(host) ||
                string.IsNullOrWhiteSpace(user) ||
                string.IsNullOrWhiteSpace(database))
            {
                throw new InvalidOperationException("Railway MySQL variables are incomplete.");
            }

            return new MySqlConnectionStringBuilder
            {
                Server = host,
                Port = uint.TryParse(port, out var parsedPort) ? parsedPort : 3306,
                UserID = user,
                Password = password,
                Database = database,
                CharacterSet = "utf8mb4",
                TreatTinyAsBoolean = true,
                SslMode = MySqlSslMode.Preferred
            }.ConnectionString;
        }
    }
}
