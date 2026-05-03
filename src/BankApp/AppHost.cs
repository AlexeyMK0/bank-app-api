using Projects;

IDistributedApplicationBuilder builder = DistributedApplication.CreateBuilder(args);

IResourceBuilder<KeycloakResource> keycloak = builder
    .AddKeycloakContainer("service-keycloak")
    .WithLifetime(ContainerLifetime.Persistent)
    .WithOtlpExporter()
    .WithDataVolume()
    .WithEnvironment("KEYCLOAK_ADMIN", "admin")
    .WithEnvironment("KEYCLOAK_ADMIN_PASSWORD", "admin")
    .WithExternalHttpEndpoints();

IResourceBuilder<KeycloakRealmResource> realm = keycloak.AddRealm("bank-app-realm");

IResourceBuilder<PostgresServerResource> postgres = builder
    .AddPostgres("service-postgres")
    .WithLifetime(ContainerLifetime.Persistent)
    .WithDataVolume("service-postgres-volume");

IResourceBuilder<PostgresDatabaseResource> database = postgres.AddDatabase("postgres");

IResourceBuilder<ProjectResource> service = builder.AddProject<Main>("main")
    .WaitFor(database)
    .WithReference(database)
    .WithEnvironment(
        "Infrastructure:Persistence:Postgres:Host",
        postgres.Resource.PrimaryEndpoint.Property(EndpointProperty.Host))
    .WithEnvironment(
        "Infrastructure:Persistence:Postgres:Port",
        postgres.Resource.PrimaryEndpoint.Property(EndpointProperty.Port))
    .WithEnvironment(
        "Infrastructure:Persistence:Postgres:Database",
        database.Resource.DatabaseName)
    .WithEnvironment(
        "Infrastructure:Persistence:Postgres:Username",
        postgres.Resource.UserNameReference)
    .WithEnvironment(
        "Infrastructure:Persistence:Postgres:Password",
        postgres.Resource.PasswordParameter)
    .WithHttpHealthCheck("/health");
/*.WithEnvironment("USE_PROMETHEUS_METRICS", "false")*/

Console.WriteLine($"gRpcEndpoint {service.GetEndpoint("gRpcEndpoint")}");

IResourceBuilder<ProjectResource> gateway = builder
    .AddProject<BankApp_Gateway>("bankapp-gateway")
    .WaitFor(service)
    .WaitFor(keycloak)
    .WithReference(service)
    .WithReference(realm)
    .WithEnvironment(
        "Infrastructure:Service:service-account:BaseAddress",
        service.GetEndpoint("gRPC"))
    .WithEnvironment(
        "Infrastructure:Service:service-invoice:BaseAddress",
        service.GetEndpoint("gRPC"))
    .WithEnvironment(
        "Infrastructure:Service:service-operation-history:BaseAddress",
        service.GetEndpoint("gRPC"))
    .WithEnvironment(
        "Infrastructure:Service:service-user:BaseAddress",
        service.GetEndpoint("gRPC"))
    .WithEnvironment(
        "Authentication__IdentityProviderUri",
        () => $"{keycloak.GetEndpoint("http").Url}/realms/bank-app-realm")
    .WithEnvironment(
        "Authentication__ClientId",
        "bank-app-gateway")
    /*.WithEnvironment(
        "Authentication__ClientSecret",
        "9eZuGWJJW11PjO49SVlJvdg4EbiaXok7")*/
    ;

builder.Build().Run();