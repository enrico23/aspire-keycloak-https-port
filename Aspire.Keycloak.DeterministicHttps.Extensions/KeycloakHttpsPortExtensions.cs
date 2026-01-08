using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Microsoft.Extensions.DependencyInjection;

namespace Aspire.Keycloak.DeterministicHttps.Extensions;

// This file contains code derived from the dotnet/aspire project.
// https://github.com/dotnet/aspire
//
// Copyright (c) .NET Foundation and Contributors.
// Licensed under the MIT License.
//
// Modifications:
// - Added optional httpsPort parameter to allow pinning the HTTPS host port for local development/demo.
// - Removed non-essential helpers to keep the demo implementation minimal.

#pragma warning disable ASPIRECERTIFICATES001
public static class KeycloakHttpsPortExtensions
{
    private const int DefaultContainerPort = 8080;
    private const int DefaultHttpsPort = 8443;
    private const int ManagementInterfaceContainerPort = 9000;
    private const string ManagementEndpointName = "management";

    private const string AdminEnvVarName = "KC_BOOTSTRAP_ADMIN_USERNAME";
    private const string AdminPasswordEnvVarName = "KC_BOOTSTRAP_ADMIN_PASSWORD";
    private const string HealthCheckEnvVarName = "KC_HEALTH_ENABLED";

    public static IResourceBuilder<KeycloakResourceV2> AddKeycloakFixedHttpsPort(
        this IDistributedApplicationBuilder builder,
        string name,
        int? httpPort = null,
        int? httpsPort = null,
        IResourceBuilder<ParameterResource>? adminUsername = null,
        IResourceBuilder<ParameterResource>? adminPassword = null)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrEmpty(name);

        var passwordParameter =
            adminPassword?.Resource
            ?? ParameterResourceBuilderExtensions.CreateDefaultPasswordParameter(builder, $"{name}-password");

        var resource = new KeycloakResourceV2(name, adminUsername?.Resource, passwordParameter);

        var keycloak = builder
            .AddResource(resource)
            .WithImage(KeycloakContainerImageTagsV2.Image)
            .WithImageRegistry(KeycloakContainerImageTagsV2.Registry)
            .WithImageTag(KeycloakContainerImageTagsV2.Tag)
            .WithHttpEndpoint(port: httpPort, targetPort: DefaultContainerPort)
            .WithHttpEndpoint(targetPort: ManagementInterfaceContainerPort, name: ManagementEndpointName)
            .WithHttpHealthCheck(endpointName: ManagementEndpointName, path: "/health/ready")
            .WithEnvironment(ctx =>
            {
                ctx.EnvironmentVariables[AdminEnvVarName] = resource.AdminReference;
                ctx.EnvironmentVariables[AdminPasswordEnvVarName] = resource.AdminPasswordParameter;
                ctx.EnvironmentVariables[HealthCheckEnvVarName] = "true";
            })
            .WithUrlForEndpoint(ManagementEndpointName, u => u.DisplayLocation = UrlDisplayLocation.DetailsOnly)
            .WithHttpsCertificateConfiguration(ctx =>
            {
                if (ctx.Password is null)
                {
                    ctx.EnvironmentVariables["KC_HTTPS_CERTIFICATE_FILE"] = ctx.CertificatePath;
                    ctx.EnvironmentVariables["KC_HTTPS_CERTIFICATE_KEY_FILE"] = ctx.KeyPath;
                }
                else
                {
                    ctx.EnvironmentVariables["KC_HTTPS_KEY_STORE_FILE"] = ctx.PfxPath;
                    ctx.EnvironmentVariables["KC_HTTPS_KEY_STORE_TYPE"] = "pkcs12";
                    ctx.EnvironmentVariables["KC_HTTPS_KEY_STORE_PASSWORD"] = ctx.Password;
                }

                return Task.CompletedTask;
            });

        if (builder.ExecutionContext.IsRunMode)
        {
            builder.Eventing.Subscribe<BeforeStartEvent>((@event, cancellationToken) =>
            {
                var devCert = @event.Services.GetRequiredService<IDeveloperCertificateService>();

                var addHttps =
                    !resource.TryGetLastAnnotation<HttpsCertificateAnnotation>(out var annotation)
                        ? devCert.UseForHttps
                        : annotation.UseDeveloperCertificate.GetValueOrDefault(devCert.UseForHttps) || annotation.Certificate is not null;

                if (addHttps)
                {
                    // Workaround: allow pinning the HTTPS host port for deterministic local authority URLs.
                    // When httpsPort is null, Aspire will select an ephemeral host port (default behavior).
                    keycloak
                        .WithHttpsEndpoint(port: httpsPort, targetPort: DefaultHttpsPort, env: "KC_HTTPS_PORT")
                        .WithEndpoint(ManagementEndpointName, ep => ep.UriScheme = "https");
                }

                return Task.CompletedTask;
            });

            keycloak.WithArgs("start-dev");
        }
        else
        {
            keycloak.WithArgs("start");
        }

        keycloak.WithArgs("--import-realm");

        return keycloak;
    }

    public static IResourceBuilder<KeycloakResourceV2> WithDataVolume(this IResourceBuilder<KeycloakResourceV2> builder, string? name = null)
    {
        ArgumentNullException.ThrowIfNull(builder);

        return builder.WithVolume(
            name ?? VolumeNameGenerator.Generate(builder, "data"),
            "/opt/keycloak/data",
            false);
    }
}
#pragma warning restore ASPIRECERTIFICATES001

internal static class KeycloakContainerImageTagsV2
{
    public const string Registry = "quay.io";
    public const string Image = "keycloak/keycloak";
    public const string Tag = "26.4";
}

public sealed class KeycloakResourceV2(string name, ParameterResource? admin, ParameterResource adminPassword)
    : ContainerResource(name), IResourceWithServiceDiscovery
{
    private const string DefaultAdmin = "admin";

    public ParameterResource? AdminUserNameParameter { get; } = admin;

    internal ReferenceExpression AdminReference =>
        AdminUserNameParameter is not null ?
            ReferenceExpression.Create($"{AdminUserNameParameter}") :
            ReferenceExpression.Create($"{DefaultAdmin}");

    public ParameterResource AdminPasswordParameter { get; } = adminPassword ??
        throw new ArgumentNullException(nameof(adminPassword));
}