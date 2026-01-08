var builder = DistributedApplication.CreateBuilder(args);

var apiService = builder.AddProject<Projects.Aspire_Keycloak_DeterministicHttps_ApiService>("apiservice")
    .WithHttpHealthCheck("/health");

builder.AddProject<Projects.Aspire_Keycloak_DeterministicHttps_Web>("webfrontend")
    .WithExternalHttpEndpoints()
    .WithHttpHealthCheck("/health")
    .WithReference(apiService)
    .WaitFor(apiService);

builder.Build().Run();
