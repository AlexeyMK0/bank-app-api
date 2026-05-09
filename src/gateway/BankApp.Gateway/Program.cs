#pragma warning disable CA1506
#pragma warning disable SA1515
/*
 * more than 41 dependencies
 */

using BankApp.Gateway.Application.Contracts;
using BankApp.Gateway.Application.Services;
using BankApp.Gateway.Infrastructure.Service;
using BankApp.Gateway.Presentation.Http;
using BankApp.Gateway.Presentation.Http.AuthorizationModels;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using System.Diagnostics;
using System.Security.Claims;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Logging.AddConsole();

builder.Services
    .AddClients()
    .AddServices()
    .AddPresentationHttp()
    .AddHttpContextAccessor();

builder.Services
    .AddAuthorization(auth =>
    {
        auth.AddPolicy(
            AppFeatures.ReadAccount,
            policy => policy
            .RequireAuthenticatedUser()
            .RequireClaim("permissions", AppFeatures.ReadAccount));

        auth.AddPolicy(
            AppFeatures.AccountDeposit,
            policy => policy
            .RequireAuthenticatedUser()
            .RequireClaim("permissions", AppFeatures.AccountDeposit));

        auth.AddPolicy(
            AppFeatures.AccountWithdraw,
            policy => policy
            .RequireAuthenticatedUser()
            .RequireClaim("permissions", AppFeatures.AccountWithdraw));

        auth.AddPolicy(
            AppFeatures.ReadAccountBalance,
            policy => policy
            .RequireAuthenticatedUser()
            .RequireClaim("permissions", AppFeatures.ReadAccountBalance));

        auth.AddPolicy(
            AppFeatures.CreateAccount,
            policy => policy
            .RequireAuthenticatedUser()
            .RequireClaim("permissions", AppFeatures.CreateAccount));

        auth.AddPolicy(
            AppFeatures.CancelInvoice,
            policy => policy
            .RequireAuthenticatedUser()
            .RequireClaim("permissions", AppFeatures.CancelInvoice));

        auth.AddPolicy(
            AppFeatures.PayInvoice,
            policy => policy
            .RequireAuthenticatedUser()
            .RequireClaim("permissions", AppFeatures.PayInvoice));

        auth.AddPolicy(
            AppFeatures.ReadInvoice,
            policy => policy
            .RequireAuthenticatedUser()
            .RequireClaim("permissions", AppFeatures.ReadInvoice));

        auth.AddPolicy(
            AppFeatures.CreateInvoice,
            policy => policy
            .RequireAuthenticatedUser()
            .RequireClaim("permissions", AppFeatures.CreateInvoice));

        auth.AddPolicy(
            AppFeatures.ReadOperation,
            policy => policy
            .RequireAuthenticatedUser()
            .RequireClaim("permissions", AppFeatures.ReadOperation));
    });

builder.Services
    .AddAuthentication(auth =>
    {
        auth.DefaultScheme = "composite";
        auth.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddPolicyScheme(
        "composite",
        "composite",
        options =>
        {
            options.ForwardDefaultSelector = context =>
            {
                if (context.Request.Headers.Authorization.ToString().StartsWith("Bearer"))
                {
                    return JwtBearerDefaults.AuthenticationScheme;
                }

                return CookieAuthenticationDefaults.AuthenticationScheme;
            };
        })
    .AddCookie(options =>
    {
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest; // debug, in prod = Always
        options.Cookie.SameSite = SameSiteMode.Lax; // debug, in prod = Strict
    })
    .AddOpenIdConnect(oidc =>
    {
        oidc.Authority = builder.Configuration["Authentication:IdentityProviderUri"];
        oidc.ClientId = builder.Configuration["Authentication:ClientId"];
        oidc.ClientSecret = builder.Configuration["Authentication:ClientSecret"];

        oidc.ResponseType = "code";
        oidc.SaveTokens = true;

        oidc.RequireHttpsMetadata = false; // debug

        oidc.TokenValidationParameters = new TokenValidationParameters
        {
            RoleClaimType = ClaimTypes.Role,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(10),
        };

        oidc.Events = new OpenIdConnectEvents
        {
            OnTokenValidated = async context =>
            {
                string token = await context.HttpContext.GetTokenAsync("access_token")
                    ?? throw new UnreachableException("Token not found");
                IUserService userService = context.HttpContext.RequestServices
                    .GetRequiredService<IUserService>();
                ILogger<IUserService> logger = context.HttpContext.RequestServices
                    .GetRequiredService<ILogger<IUserService>>();
                long userId = await userService.AddUserAsync(Guid.Parse(token), context.HttpContext.RequestAborted);
                logger.LogInformation($"Token validated for user {userId}");
            },
        };
    })
    .AddJwtBearer(jwt =>
    {
        jwt.Authority = builder.Configuration["Authentication:IdentityProviderUri"];
        jwt.Audience = "account";
        jwt.ClaimsIssuer = "master";

        jwt.RequireHttpsMetadata = false; // debug

        jwt.TokenValidationParameters = new TokenValidationParameters
        {
            RoleClaimType = ClaimTypes.Role,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(10),
        };

        jwt.Events = new JwtBearerEvents
        {
            OnTokenValidated = async context =>
            {
                string token = context.Principal?.FindFirst(ClaimTypes.NameIdentifier)?.Value
                               ?? throw new UnreachableException("Token not found");
                IUserService userService = context.HttpContext.RequestServices
                    .GetRequiredService<IUserService>();
                ILogger<IUserService> logger = context.HttpContext.RequestServices
                    .GetRequiredService<ILogger<IUserService>>();
                long userId = await userService.AddUserAsync(Guid.Parse(token), context.HttpContext.RequestAborted);
                logger.LogInformation($"Token validated for user {userId}");
            },
        };
    });

builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(swagger =>
{
    swagger.AddSecurityDefinition(
        "oidc",
        new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.OAuth2,
            Flows = new OpenApiOAuthFlows
            {
                AuthorizationCode = new OpenApiOAuthFlow
                {
                    AuthorizationUrl =
                        new Uri(
                            $"{builder.Configuration["Authentication:IdentityProviderUri"]}/protocol/openid-connect/auth"),
                    TokenUrl = new Uri(
                        $"{builder.Configuration["Authentication:IdentityProviderUri"]}/protocol/openid-connect/token"),
                },
            },
        });

    swagger.AddSecurityRequirement(doc => new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecuritySchemeReference("oidc", doc),
            ["openid", "profile"]
        },
    });
});

WebApplication app = builder.Build();

app.UseRpcExceptionMiddleware();
app.MapDefaultEndpoints();

ILogger<Program> logger = app.Services.GetRequiredService<ILogger<Program>>();
logger.LogInformation("application.Environment.IsDevelopment(): {IsDevelopment}", app.Environment.IsDevelopment());
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(swagger =>
    {
        swagger.RoutePrefix = "aboba"; // specifies path to ui
        swagger.SwaggerEndpoint("/swagger/v1/swagger.json", "BankApp Gateway v1"); // specifies path to JSON
        swagger.OAuthClientId(builder.Configuration["Authentication:ClientId"] + "-swagger");
        swagger.OAuthUsePkce();
    });
}

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.UsePresentationHttp();

app.Run();