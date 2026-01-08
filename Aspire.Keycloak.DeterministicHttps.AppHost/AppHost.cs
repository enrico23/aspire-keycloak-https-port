using Aspire.Keycloak.DeterministicHttps.Extensions;

var builder = DistributedApplication.CreateBuilder(args);

var username = builder.AddParameter("username");
var password = builder.AddParameter("password", secret: true);

var keycloak = builder.AddKeycloakFixedHttpsPort("keycloak", httpPort: 8080, httpsPort: 8093, username, password)
    .WithDataVolume("keycloak");

var apiService = builder.AddProject<Projects.Aspire_Keycloak_DeterministicHttps_ApiService> ("apiservice")
    .WithHttpHealthCheck("/health");

builder.Build().Run();
