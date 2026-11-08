---
description: Plan and build one vertical slice that works locally end to end
---
Feature to build: $ARGUMENTS

1. Read @docs/spec.md, @docs/architecture.md and the standards in docs/standards/.
2. Propose a plan: endpoints, application use cases, domain changes, UI screens, caching, notifications, and the tests you will add. Stop and wait for my approval.
3. Create branch `feature/<issue>-<kebab-description>`.
4. Implement following the naming and architecture standards. Keep layers loosely coupled through the ports in architecture.md.
5. Run it with `aspire run` and verify the slice end to end, in light and dark themes.
6. Report what you built, how you verified it, and anything left open. Do not add tests yet unless I ask; use /add-tests next.
