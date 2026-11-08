---
description: Regenerate the architecture diagrams and the README architecture section from the current code
---
Diagrams live in `docs/diagrams/` (see its README.md for the file list). Sources are code; images are rendered from them.

1. **Inventory first.** Read the solution, `src/<Product>.AppHost` (Aspire), `infra/` Terraform, the message contracts and the auth setup. List every service, data store, cache, queue, external system and main flow, and how they call each other. Show me the list and stop. I'll correct it before you draw anything.
2. **Update the sources** once I confirm:
   - `context.mmd`: people, this system, external systems.
   - `containers.mmd`: frontend, API, workers, databases, cache, queues, identity provider.
   - `sequence-login.mmd` plus one `sequence-<flow>.mmd` per main business flow.
   - `state-<entity>.mmd` for each entity with a lifecycle (status field with transitions).
   - `deployment-azure.py` (and `deployment-aws.py` if the repo deploys to AWS) from `infra/`.
   Mermaid flowcharts only (no experimental C4 syntax). Use the `ours` and `ext` classDefs from `containers.mmd`, a Legend subgraph on every view, at most about 12 boxes per view. Split rather than crowd. Don't use angle brackets in labels.
3. **Render**: run `pwsh -File scripts/render-diagrams.ps1`. Fix any syntax error it reports and run it again.
4. **Check the images**: open each changed .svg and .png and check that labels are readable, nothing overlaps and arrows point the right way. Fix and re-render.
5. **Update README.md's Architecture section**: keep the image links at their fixed paths, add or remove rows in its diagram table so it matches the files in `docs/diagrams/`, and update the one-line descriptions. Update the Tech stack line and the "Run it locally" secrets table if a service or secret was added or removed.
6. **Commit** sources, images and README together: `docs: update architecture diagrams`. Report what changed.
