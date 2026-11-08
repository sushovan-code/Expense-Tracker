# 0003. Frontend libraries differ from the standard's defaults

- Status: Accepted
- Date: 2026-11-08

## Context
`docs/standards/coding-standards.md` and `ui-and-theming.md` name React Hook Form with schema validation, AG Grid, react-toastify or Sonner, and ESLint with Prettier. The web app uses:
- hand-written validation (`features/*/model/validation.ts`)
- TanStack Table with the shared grid components (`shared/ui/grid`)
- its own toast system (`shared/toast`)
- oxlint

## Decision
Keep the current libraries.
- The look and feel is fixed: the owner asked that the current design, flows, themes and colours stay as they are. Replacing the grid or the toasts would change how they look and behave.
- The grid and validation already meet the standard's requirements:
  - **Grid:** server-side sorting, filtering and paging, plus CSV export.
  - **Validation:** pure, unit-tested functions mirroring the API's rules.
- The toasts are token-styled in both themes, but they differ from `ui-and-theming.md` in three ways (`shared/toast/ToastProvider.tsx`):
  - At most **4** are visible, not 5.
  - When a new one arrives, the oldest is **dropped**, not queued.
  - Errors **auto-dismiss after 7 seconds** instead of staying until dismissed.

  These gaps are accepted for now, because changing them changes visible behaviour. If the owner approves, closing them is a small change inside `ToastProvider`.
- oxlint enforces the folder boundaries and `import/no-cycle` (`.oxlintrc.json`) and runs much faster than ESLint. TypeScript strict mode covers what typed linting would add.

## Consequences
- There are no extra dependencies to maintain, and the UI stays the same.
- New forms follow the existing ViewModel and validation pattern instead of React Hook Form. If forms grow complex (receipt upload, recurring rules), reconsider React Hook Form together with a schema library and write a follow-up ADR.
- Xlsx export (roadmap 4.6) will need a server-side library; that's a separate decision.
