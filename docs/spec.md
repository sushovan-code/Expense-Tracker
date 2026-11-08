# Product spec: Expense Tracker

## Purpose
A family records what it spends, sees where the money goes and stays within monthly budgets. Every signed-in session sees changes live. The data is synthetic; this is a portfolio project.

## Users and access
- **App roles** (existing RBAC): SuperAdmin > Admin > User > Reader. Each holds `resource.action` permissions. These decide what someone may do in the app at all. See the security section of `architecture.md`.
- **Family roles** (planned, `roadmap.md` slice 2): Owner, Member or Viewer within one family. These decide whose data you see and what you may do inside your family. A user belongs to at most one family.
- Sign-in uses email and password with a short-lived JWT access token and a rotating refresh cookie, or a personal API key.

## What it does today
- **Dashboard**:
  - a month picker
  - totals compared with the previous month
  - spending by category (donut chart) and a 6-month trend (bar chart)
  - budget progress and recent expenses
- **Expenses**:
  - a grid with server-side paging, sorting and filtering (date range, category, search)
  - inline editing, bulk delete and CSV export
  - delete with confirmation, then undo
- **Categories**: name, colour and an optional monthly budget. A category that still has expenses can't be deleted.
- **Budgets**: each category is shown as on track, close to its limit (80% or more) or over budget. An alert goes out the moment a category goes over.
- **Realtime**: SignalR pushes changes and budget alerts to every open session.
- **Account**: change password, manage personal API keys.
- **Administration** (Admin or above): users, role assignment, direct permission claims, role permissions, unlock, sign out everywhere.
- **Themes**: Light, Dark and System.

## Rules
- An expense has a description (1–200 characters), an amount from 0.01 to 999,999,999 with at most 2 decimal places, a date, a category and optional notes (up to 1,000 characters).
- A category has a unique name (1–50 characters), a hex colour and an optional budget from 0 to 999,999,999.
- Lists are always paged (20 per page by default, 100 at most). Exports are capped at 10,000 rows.

## Planned
See `roadmap.md`:
- family groups with invites and private expenses
- a QR code on the login page
- budget alerts at 75%, 90% and 100%
- income and savings goals
- recurring expenses
- payment method and "paid by"
- month-on-month trends
- amount filters and Xlsx export
- receipt photos
- an activity log
- custom category icons
- a monthly summary
