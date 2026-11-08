# 0006. URL-path API versioning

- Status: Accepted
- Date: 2026-11-08

## Context
The naming standard asks for versioned routes (`/api/v1/<plural-kebab-noun>`). The API served everything under `/api`. The refresh-token cookie is scoped to the auth endpoints' path, so moving the routes also moves the cookie.

## Decision
- Every REST endpoint is mapped under one constant, `ApiRoutes.Prefix = "/api/v1"`. A breaking change gets a `/api/v2` group mapped alongside, and v1 stays until clients move.
- The refresh cookie's path is `ApiRoutes.Auth` (`/api/v1/auth`), so it's still sent only to the auth endpoints. The JWT and refresh flow is otherwise unchanged: same lifetimes, rotation, reuse detection, HttpOnly, Secure and SameSite=Strict.
- The SignalR hub (`/hubs/expenses`) and the health checks stay unversioned: they're infrastructure, not the resource contract.
- The web app's base URL is `VITE_API_BASE_URL=/api/v1`; the dev proxy still forwards all of `/api`.

## Consequences
- Old `/api/...` URLs now return 404. Scripts that use API keys must switch to `/api/v1`.
- A refresh cookie issued before this change has the old path, so the browser won't send it to `/api/v1/auth/refresh`: signed-in users sign in once more after the upgrade. The old cookie expires on its own within 7 days.
- URL versioning is visible in logs, docs and the OpenAPI document (already named `v1`), and needs no extra package.
