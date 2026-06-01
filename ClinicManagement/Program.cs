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
            try
            {
                Run(args);
            }
            catch (Exception ex) when (IsRailwayRuntime())
            {
                Console.Error.WriteLine(BuildConciseStartupError(ex));

                // Slow Railway crash loops so one bad startup does not flood deployment logs.
                System.Threading.Thread.Sleep(TimeSpan.FromSeconds(20));
                Environment.ExitCode = 1;
            }
        }

        private static void Run(string[] args)
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

        private static bool IsRailwayRuntime()
        {
            return !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("RAILWAY_ENVIRONMENT")) ||
                   !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("RAILWAY_PROJECT_ID")) ||
                   !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("RAILWAY_SERVICE_ID"));
        }

        private static string BuildConciseStartupError(Exception exception)
        {
            var messages = new List<string>();
            for (var current = exception; current is not null; current = current.InnerException)
            {
                messages.Add($"{current.GetType().Name}: {current.Message}");
            }

            return "Application startup failed. " + string.Join(" | Inner: ", messages);
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
            var railwayUrl = ReadSetting(configuration, "MYSQL_URL");

            if (!string.IsNullOrWhiteSpace(railwayUrl))
            {
                if (!Uri.TryCreate(railwayUrl, UriKind.Absolute, out var mysqlUri))
                {
                    throw new InvalidOperationException(
                        "MYSQL_URL is present but is not a valid absolute MySQL URL.");
                }

                var userInfo = mysqlUri.UserInfo.Split(':', 2);
                return BuildMySqlConnectionString(
                    mysqlUri.Host,
                    mysqlUri.Port > 0 ? mysqlUri.Port.ToString() : "3306",
                    Uri.UnescapeDataString(userInfo.ElementAtOrDefault(0) ?? string.Empty),
                    Uri.UnescapeDataString(userInfo.ElementAtOrDefault(1) ?? string.Empty),
                    mysqlUri.AbsolutePath.Trim('/'));
            }

            var mysqlHost = ReadSetting(configuration, "MYSQLHOST");
            if (!string.IsNullOrWhiteSpace(mysqlHost))
            {
                return BuildMySqlConnectionString(
                    mysqlHost,
                    ReadSetting(configuration, "MYSQLPORT") ?? "3306",
                    ReadSetting(configuration, "MYSQLUSER") ?? string.Empty,
                    ReadSetting(configuration, "MYSQLPASSWORD") ?? string.Empty,
                    ReadSetting(configuration, "MYSQLDATABASE") ?? string.Empty);
            }

            if (IsHostedProduction(configuration, environment))
            {
                throw new InvalidOperationException(
                    "Missing MySQL configuration for hosted Production. Add MYSQL_URL or MYSQLHOST/MYSQLPORT/MYSQLUSER/MYSQLPASSWORD/MYSQLDATABASE to the web service.");
            }

            return configuration.GetConnectionString("DefaultConnection")
                   ?? throw new InvalidOperationException("Missing MySQL connection configuration.");
        }

        private static string? ReadSetting(IConfiguration configuration, string key)
        {
            return CleanSettingValue(configuration[key]);
        }

        private static string? CleanSettingValue(string? value)
        {
            return string.IsNullOrWhiteSpace(value)
                ? null
                : value.Trim().Trim('"');
        }

        private static bool IsHostedProduction(IConfiguration configuration, IWebHostEnvironment environment)
        {
            return !string.IsNullOrWhiteSpace(configuration["RAILWAY_ENVIRONMENT"]) ||
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
            host = CleanSettingValue(host) ?? string.Empty;
            port = CleanSettingValue(port) ?? string.Empty;
            user = CleanSettingValue(user) ?? string.Empty;
            password = CleanSettingValue(password) ?? string.Empty;
            database = CleanSettingValue(database) ?? string.Empty;

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
