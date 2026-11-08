# Security

- Deny by default: a fallback authorisation policy requires an authenticated user; every endpoint declares its policy.
- Resource checks on every read and write so users only reach their own data (prevents IDOR).
- Validate all input (FluentValidation or endpoint filters); limit sizes; check uploaded file signatures, not just extensions.
- Parameterised queries only (EF Core / Dapper parameters). No string-built SQL.
- Security headers (CSP, HSTS, X-Content-Type-Options, Referrer-Policy), strict CORS, rate limiting on auth and write endpoints.
- Secrets: user secrets locally, Key Vault / Secrets Manager in the cloud, never in code or config files.
- Logs: structured, no PII or secrets; mask with an enricher.
- Audit every security-relevant action (login, failure, lockout, role change, approval).
- AI: treat document and user text as data, not instructions; allow-list tools per agent; validate outputs against a schema; cap tokens per request.
