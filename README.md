# Aspire Keycloak HTTPS Port Determinism

This repository demonstrates a **temporary workaround** 
for configuring a deterministic HTTPS authority port when 
running **Keycloak with .NET Aspire** in local development 
scenarios.

## Background

The current Aspire Keycloak hosting integration provisions 
an HTTPS endpoint with a dynamically assigned host port. 
While this behavior is reasonable and generally safe, 
it introduces friction in local development when working with:

- Interactive or external OAuth/OIDC clients
- Manual token acquisition and debugging
- API gateways or reverse proxies that cache OIDC metadata
- Scripts or documentation that expect a stable authority URL

This repository exists to document and demonstrate a pragmatic 
mitigation while an upstream solution is discussed.

## Scope

- The solution is intentionally **narrow in scope** 
- and focused on local development ergonomics.
- A minimal Aspire stack (AppHost, API) is included 
- **only to demonstrate and validate the behavior**.
- No production usage is implied or recommended.
