---
description: Self-review the branch and draft the pull request
---
1. Run `dotnet build`, `dotnet test`, `dotnet format --verify-no-changes`, and in the frontend folder `npm run lint`, `npm run test:ci` and `npm run build`. Fix anything that fails. Warnings don't fail the build: list the ones this branch introduced and fix them, or say why one should stay.
2. Use the security-reviewer and ui-reviewer subagents on the diff versus main; fix confirmed issues.
3. If this branch added, removed or rewired a service, data store, cache, queue, external system or main flow, run the steps in @.claude/commands/update-diagrams.md. Check that README.md's "Run it locally" section still works for someone who has just cloned the repo: prerequisites, secrets table, URLs and test users must match the code.
4. Check the code review checklist in @docs/standards/coding-standards.md and the definition of done in @docs/standards/development-workflow.md, and list any unmet item.
5. Commit any remaining changes with Conventional Commit messages.
6. Write the PR title (Conventional Commits, one line) to `.claude/pr-title.txt` and the description (following .github/pull_request_template.md, with "Closes #<issue>") to `.claude/pr-draft.md`. Do not push or open the PR; show me both.
