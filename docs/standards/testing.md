# Testing

Tests are written in stage 1 after the slice works locally, and must pass before the PR.

## Backend
- **Unit tests** (`tests/<Product>.<Module>.UnitTests`): xUnit, NSubstitute for test doubles, Shouldly for assertions. Cover domain rules and application handlers. Use `FakeTimeProvider` for time.
- **Integration tests** (`tests/<Product>.<Module>.IntegrationTests`): `WebApplicationFactory` with Testcontainers (SQL Server, Postgres, Redis) and Respawn to reset data. Test real HTTP endpoints, auth policies and persistence.
- Every endpoint has a test proving unauthenticated and unauthorised calls are rejected.
- AI features: deterministic tests with a fake `IChatClient`; separate evaluation suite against a real model, run nightly.

## Frontend
- **Angular**: component and service tests with the Angular CLI's configured runner (Vitest or Jest) and Angular Testing Library; HTTP mocked with `provideHttpClientTesting`.
- **React**: Vitest + React Testing Library; API mocked with MSW.
- Test behaviour the user sees (text, roles, interactions), not implementation details.
- Optional end-to-end: Playwright for the main journeys, in light and dark themes.

## Standards
- Name: `Method_State_ExpectedResult`; Arrange / Act / Assert.
- Coverage target: 80% for Domain and Application; no target for generated code.
- No test depends on another test, on the clock, or on the network outside Testcontainers.
