var builder = DistributedApplication.CreateBuilder(args);

var apiService = builder.AddProject<Projects.Aspire_Keycloak_DeterministicHttps_ApiService>("apiservice")
    .WithHttpHealthCheck("/health");

builder.Build().Run();
