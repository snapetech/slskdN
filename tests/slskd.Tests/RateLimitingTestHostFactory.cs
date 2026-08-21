// <copyright file="RateLimitingTestHostFactory.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>
namespace slskd.Tests;

using System;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Threading.RateLimiting;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Asp.Versioning;
using slskd;
using slskd.Authentication;
using slskd.Core.API;
using slskd.Core.Security;

/// <summary>
/// Test host for PR-09 rate limiting: RateLimiting.Enabled=true, ApiPermitLimit=2, ApiWindowSeconds=60.
/// Burst of 3 to /api/v0/session/enabled → 3rd returns 429.
/// </summary>
public class RateLimitingTestHostFactory : WebApplicationFactory<ProgramStub>
{
    private const string TestApiKeyScheme = "TestApiKey";
    private const string TestSelectorScheme = "TestApiKeyOrJwt";

    protected override IHostBuilder CreateHostBuilder()
    {
        var solutionRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
        var contentRoot = Path.Combine(solutionRoot, "tests", "slskd.Tests");
        Directory.CreateDirectory(contentRoot);
        Directory.CreateDirectory(Path.Combine(solutionRoot, "slskd.Tests"));

        var rateLimiting = new slskd.Options.WebOptions.RateLimitingOptions
        {
            Enabled = true,
            ApiPermitLimit = 2,
            ApiWindowSeconds = 60,
            FederationPermitLimit = 30,
            FederationWindowSeconds = 60,
            MeshGatewayPermitLimit = 60,
            MeshGatewayWindowSeconds = 60,
        };
        var optionsAtStartup = new OptionsAtStartup
        {
            Web = new slskd.Options.WebOptions
            {
                EnforceSecurity = true,
                RateLimiting = rateLimiting,
            },
            Headless = false,
        };

        var apiPermit = rateLimiting.ApiPermitLimit;
        var apiWindow = TimeSpan.FromSeconds(rateLimiting.ApiWindowSeconds <= 0 ? 60 : rateLimiting.ApiWindowSeconds);

        return new HostBuilder()
            .UseContentRoot(contentRoot)
            .UseEnvironment("Test")
            .ConfigureWebHostDefaults(web =>
            {
                web.UseTestServer();
                web.UseContentRoot(contentRoot);
                web.ConfigureServices(services =>
                {
                    services.AddSingleton(optionsAtStartup);
                    services.AddOptions<slskd.Options>();
                    services.AddSingleton<IOptionsSnapshot<slskd.Options>>(sp =>
                        new StaticOptionsSnapshot(sp.GetRequiredService<IOptions<slskd.Options>>().Value));
                    services.AddSingleton<ISecurityService, StubSecurityService>();

                    services.AddApiVersioning(o =>
                    {
                        o.DefaultApiVersion = new ApiVersion(0, 0);
                        o.AssumeDefaultVersionWhenUnspecified = true;
                        o.ReportApiVersions = true;
                    }).AddMvc().AddApiExplorer(o =>
                    {
                        o.GroupNameFormat = "'v'VVV";
                        o.SubstituteApiVersionInUrl = true;
                    });

                    var fedPermit = rateLimiting.FederationPermitLimit;
                    var fedWindow = TimeSpan.FromSeconds(rateLimiting.FederationWindowSeconds <= 0 ? 60 : rateLimiting.FederationWindowSeconds);
                    var meshPermit = rateLimiting.MeshGatewayPermitLimit;
                    var meshWindow = TimeSpan.FromSeconds(rateLimiting.MeshGatewayWindowSeconds <= 0 ? 60 : rateLimiting.MeshGatewayWindowSeconds);

                    services.AddRateLimiter(o =>
                    {
                        o.RejectionStatusCode = 429;
                        o.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
                        {
                            var path = context.Request.Path.Value ?? "";
                            var ip = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
                            if (path.StartsWith("/mesh/", StringComparison.OrdinalIgnoreCase))
                                return RateLimitPartition.GetFixedWindowLimiter("mesh:" + ip, _ => new FixedWindowRateLimiterOptions { PermitLimit = meshPermit, Window = meshWindow });
                            if (path.Contains("/inbox", StringComparison.OrdinalIgnoreCase) && string.Equals(context.Request.Method, "POST", StringComparison.OrdinalIgnoreCase))
                                return RateLimitPartition.GetFixedWindowLimiter("fed:" + ip, _ => new FixedWindowRateLimiterOptions { PermitLimit = fedPermit, Window = fedWindow });
                            if (context.User.Identity?.IsAuthenticated == true)
                                return RateLimitPartition.GetNoLimiter("authenticated");
                            if (!path.StartsWith("/api/", StringComparison.OrdinalIgnoreCase))
                                return RateLimitPartition.GetNoLimiter("web");
                            return RateLimitPartition.GetFixedWindowLimiter("api:" + ip, _ => new FixedWindowRateLimiterOptions { PermitLimit = apiPermit, Window = apiWindow });
                        });
                    });

                    services.AddAuthentication(options =>
                        {
                            options.DefaultAuthenticateScheme = TestSelectorScheme;
                            options.DefaultChallengeScheme = TestSelectorScheme;
                        })
                        .AddPolicyScheme(TestSelectorScheme, TestSelectorScheme, options =>
                            options.ForwardDefaultSelector = context =>
                                context.Request.Headers.ContainsKey("X-API-Key")
                                    ? TestApiKeyScheme
                                    : JwtBearerDefaults.AuthenticationScheme)
                        .AddJwtBearer()
                        .AddScheme<AuthenticationSchemeOptions, TestApiKeyAuthenticationHandler>(TestApiKeyScheme, _ => { });
                    services.AddAuthorization(o =>
                        o.AddPolicy(AuthPolicy.Any, p => p.RequireAuthenticatedUser()));

                    services.AddControllers(o => o.Filters.Add(new AuthorizeFilter(AuthPolicy.Any)))
                        .ConfigureApplicationPartManager(manager =>
                        {
                            var existing = manager.FeatureProviders
                                .OfType<IApplicationFeatureProvider<ControllerFeature>>().ToList();
                            foreach (var provider in existing)
                            {
                                manager.FeatureProviders.Remove(provider);
                            }

                            manager.FeatureProviders.Add(new slskd.Common.CodeQuality.SafeControllerFeatureProvider());
                        })
                        .ConfigureApiBehaviorOptions(o =>
                            o.SuppressModelStateInvalidFilter = !optionsAtStartup.Web.EnforceSecurity)
                        .AddApplicationPart(typeof(SessionController).Assembly);
                });
                web.Configure(app =>
                {
                    app.UseRouting();
                    app.UseAuthentication();
                    app.UseRateLimiter();
                    app.UseAuthorization();
                    app.UseEndpoints(e => e.MapControllers());
                });
            });
    }

    private sealed class TestApiKeyAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
    {
        public TestApiKeyAuthenticationHandler(
            IOptionsMonitor<AuthenticationSchemeOptions> options,
            ILoggerFactory logger,
            UrlEncoder encoder)
            : base(options, logger, encoder)
        {
        }

        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            if (Request.Headers["X-API-Key"] != "configured-key")
            {
                return Task.FromResult(AuthenticateResult.Fail("Invalid API key"));
            }

            var identity = new ClaimsIdentity(
                new[] { new Claim(ClaimTypes.Name, "rate-limit-test") },
                TestApiKeyScheme);
            var principal = new ClaimsPrincipal(identity);
            return Task.FromResult(AuthenticateResult.Success(
                new AuthenticationTicket(principal, TestApiKeyScheme)));
        }
    }
}
