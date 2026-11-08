# Development workflow

Work happens in two stages. Stage 2 is optional per change.

## Stage 1: local, tested, merged

1. **Plan**: in plan mode, propose the slice (endpoints, UI, data, tests). Wait for approval.
2. **Branch**: `feature/<issue-number>-<short-kebab-description>` (or `fix/`, `chore/`) from `main`.
3. **Build it to work locally end to end**: `aspire run` starts the API, frontend and every local dependency (SQL Server, Postgres with pgvector, Redis, Azurite, Service Bus emulator, Keycloak, Seq as the repo needs). Cloud-only services (Auth0, LLM APIs, Atlas) read their values from user secrets.
4. **Add tests** once the slice works: backend unit and integration tests, frontend unit tests (see `testing.md`). All must pass locally.
5. **Diagrams and README**: if the slice changed the architecture (a service, data store, cache, queue, external system or main flow), run `/update-diagrams`. Keep README.md's "Run it locally" section true.
6. **Self-review**: run `/prepare-pr`; fix what it finds.
7. **Pull request** to `main` (trunk-based, short-lived branches). CI runs build, tests, lint and security scans on every PR; this needs no cloud resources, so it always runs.
8. **Merge** with squash once checks are green.

### Definition of done (stage 1)
- [ ] Runs end to end locally with `aspire run`, in light and dark themes
- [ ] Unit and integration tests for backend and frontend, all passing
- [ ] Warnings reviewed: the ones this change introduced are fixed or explained (they don't fail the build); `dotnet format` and lint clean
- [ ] README updated: features, "Run it locally" (prerequisites, clone, secrets, run, URLs, test users, tests), screenshots
- [ ] Diagrams in `docs/diagrams/` re-rendered if the architecture changed, committed with their sources
- [ ] ADR written if a decision was made
- [ ] No secrets in the diff

## Stage 2 (optional): deploy

Deployment workflows are manual (`workflow_dispatch`), so skipping stage 2 never blocks a merge.
- Infrastructure: `platform-infra` Terraform (`infra-plan`, `infra-up`, `infra-down`).
- App: the repo's `deploy.yml` builds the image, pushes to GHCR and deploys to the chosen environment.
- Use `/deploy-plan` to have Claude prepare the change; it never applies infrastructure itself.

## README rules (every repo)

A reviewer must be able to clone the repo and run it from the README alone, without asking you anything. README.md always has these sections, in this order (template: the kit's `repo-template/README.md`):

1. Title, one-line purpose, CI badge, one screenshot
2. What it does
3. Architecture: the container diagram image and a table linking every diagram in `docs/diagrams/` (kept current by `/update-diagrams`)
4. Tech stack
5. **Run it locally**: prerequisites with versions, clone, secrets (`dotnet user-secrets`, never a committed file; a table of each secret, what it's for and where to get it), `aspire run`, the local URLs, test users from local seed data, how to run the tests, troubleshooting
6. Deploy (optional): link to `docs/deploy.md`
7. Licence

Test it: once per release, clone into a fresh folder and follow the README word for word.
