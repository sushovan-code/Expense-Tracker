---
description: Reproduce a bug with a failing test, fix it, and prove the fix
---
Bug: $ARGUMENTS

1. Create branch `fix/<issue>-<kebab-description>`.
2. Find the cause. Write a test that reproduces the bug and fails. Show me the failure.
3. Fix the cause (not the symptom), following @docs/standards/coding-standards.md.
4. Run the new test and the full suite; all must pass. Never skip or weaken a test.
5. Summarise the cause, the fix and any related risk.
