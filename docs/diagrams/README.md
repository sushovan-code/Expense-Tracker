# Diagrams

Every diagram here is code. Edit the source, run `pwsh -File scripts/render-diagrams.ps1` from the repo root, and commit the source and the rendered image together.

| Source (you or Claude edit) | Rendered (committed, shown in README and portfolio) | Tool |
| --- | --- | --- |
| `context.mmd` | `context.svg` | Mermaid: system context (people, this system, external systems) |
| `containers.mmd` | `containers.svg` | Mermaid: containers (frontend, API, workers, databases, cache, queues) |
| `sequence-<flow>.mmd` | `sequence-<flow>.svg` | Mermaid: one per main flow (login, the main business flow) |
| `state-<entity>.mmd` | `state-<entity>.svg` | Mermaid: one per entity with a lifecycle |
| `deployment-azure.py` / `deployment-aws.py` | `deployment-azure.png` / `deployment-aws.png` | Python `diagrams` + Graphviz: cloud view with official icons |
| `screenshots/*.png` | (same) | Taken by hand: Aspire dashboard graph and a Grafana trace, as proof the diagrams match reality |

`mermaid-config.json` holds the shared style (one accent colour plus greys). `puppeteer-config.json` lets the renderer run on the CI runner.

Rules: a legend on every view, at most about 12 boxes per view, split into several views rather than one giant picture.
