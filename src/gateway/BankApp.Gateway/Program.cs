#pragma warning disable CA1506
#pragma warning disable SA1515
/*
 * more than 41 dependencies
 */

using BankApp.Gateway.Infrastructure.Service;
using BankApp.Gateway.Presentation.Http;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Logging.AddConsole();

builder.Services
    .AddClients()
    .AddPresentationHttp()
    // .AddOpenApi()
    .AddHttpContextAccessor()
    .AddAuthorization();

builder.Services
    .AddAuthentication(auth =>
    {
        // name of our custom scheme
        auth.DefaultScheme = "composite";
        auth.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddPolicyScheme(
        "composite",
        "composite",
        options =>
        {
            // на основе запроса позволяет выбрать какая политика будет использоваться
            // нужно, чтобы можно было авторизоваться в swagger
            options.ForwardDefaultSelector = context =>
            {
                // если есть токен, начинающийся с Bearer
                if (context.Request.Headers.Authorization.ToString().StartsWith("Bearer"))
                {
                    // используем авторизацию по Bearer
                    return JwtBearerDefaults.AuthenticationScheme;
                }

                // иначе авторизуемся через Cookie
                return CookieAuthenticationDefaults.AuthenticationScheme;

                /*
                 * Дальше настроили swagger
                 * Он будет отправлять через Bearer
                 * Запрос просто через браузер идут через cookie
                 */
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
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(10),
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
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(10),
        };
    });

builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(swagger =>
{
    // Добавляет кнопку авторизации
    swagger.AddSecurityDefinition(
        "oidc",
        new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.OAuth2,
            Flows = new OpenApiOAuthFlows
            {
                // упрощенный поток, когда клиент -- это браузер
                AuthorizationCode = new OpenApiOAuthFlow
                {
                    // url на keyCloak, нужен, чтобы перенаправить туда для авторизации
                    AuthorizationUrl =
                        new Uri(
                            $"{builder.Configuration["Authentication:IdentityProviderUri"]}/protocol/openid-connect/auth"),
                    // где брать url
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
        swagger.SwaggerEndpoint("/swagger/v1/swagger.json", "BankApp Gateway v1"); // specifies path to JSON
        swagger.RoutePrefix = "aboba"; // specifies path to ui
        swagger.OAuthClientId(builder.Configuration["Authentication:ClientId"] + "-swagger");
        /* так клиент это браузер, то не можем передать туда пользовательские секреты
         proof key for code exchange */
        swagger.OAuthUsePkce();
    });
}

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.UsePresentationHttp();

app.Run();