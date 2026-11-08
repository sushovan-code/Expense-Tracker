# UI, design and theming

Look: a modern, restrained enterprise console. Mostly neutrals, one accent colour, colour used only to carry meaning.

## Theme modes: Light, Dark, System
- A theme switcher in the top bar (and user menu) offers **Light**, **Dark** and **System**; default is System.
- System follows `prefers-color-scheme` and updates live when the OS setting changes.
- The choice is saved in `localStorage` (`<product>.theme`) and applied as `data-theme="light|dark"` on `<html>` (no attribute = System).
- Apply it before first paint with a tiny inline script in `index.html`, so there is no flash of the wrong theme.
- All colours are CSS custom properties (design tokens) defined for both themes; components never use literal colours.
- Angular Material 3: define light and dark themes and switch with `color-scheme`. React with shadcn/ui and Tailwind: `darkMode` driven by the same `data-theme` attribute.
- Charts, data grids, toasts and diagrams read the same tokens, so they switch too.

## Design rules
- Neutral grey/slate scale; one accent for primary actions and focus; semantic colours only for status (success, info, warning, error), always with an icon or sign.
- One sans-serif family, about 5 type sizes, tabular numerals for amounts, numbers right-aligned.
- 8 px spacing grid; left navigation, top bar; thin borders over heavy shadows.
- Data grids (AG Grid) with sorting, filtering and export; skeleton loaders; designed empty and error states.
- Motion 150 to 200 ms, respects `prefers-reduced-motion`.
- WCAG 2.2 AA contrast in both themes, full keyboard navigation, visible focus rings.

## Notifications
- SignalR only. Toasts stack newest on top, max 5 visible, the rest queued; errors stay until dismissed; a bell opens the notification history.
- Angular: ngx-toastr (`maxOpened: 5`, `newestOnTop`, `progressBar`, `preventDuplicates`) with a token-styled toast component. React: react-toastify (`limit={5}`) or Sonner (`visibleToasts={5}`).

## Checking
Before a UI change is done, open it with the Playwright MCP server in light and dark themes, at 1440 px and 390 px widths, and fix anything that is clipped, low-contrast or misaligned.
