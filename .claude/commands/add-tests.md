---
description: Add unit and integration tests for the current slice, backend and frontend
---
Add tests for: $ARGUMENTS (default: the changes on this branch versus main).

Follow @docs/standards/testing.md.
- Backend unit tests for domain rules and handlers; integration tests with WebApplicationFactory and Testcontainers, including auth rejection cases.
- Frontend unit tests for new components, services and hooks.
Run all tests, fix failures in the code or tests (never delete or skip a failing test to get green), and report the results and coverage.
