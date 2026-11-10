# 0008. Self-registration and several families per person

- Status: Accepted
- Date: 2026-11-10
- Changes ADR 0007: a person is no longer limited to one family.

## Context
ADR 0007 gave each person at most one family, and accounts could only be created by an administrator, an Owner or an invite. That left no way to start from nothing, and no way to keep, say, "me and my wife" apart from "my parents".

## Decision
- **Anyone can register.** `POST /api/v1/auth/register` is anonymous and uses the existing auth rate limit. It creates an account with the default app role `User` through the same identity port as every other new user, and refuses an email that is already registered with a field error on `email`. Registering does not sign anyone in and does not touch login, refresh, tokens or RBAC.
- **Own any number of families.** Creating a family is open to everyone, and the creator is its Owner. The unique index on `FamilyMember.UserId` became unique `(FamilyId, UserId)`.
- **Member or Viewer in at most one.** `FamilyPolicy.AllowsMembership` lets a person own any number of families but be a Member or Viewer in only one. It is checked when joining with an invite and when an Owner is demoted. An Owner can still join another family as an Owner.
- **One active family per request.** The browser sends `X-Family-Id` (or `familyId` on the SignalR connection). `FamilyContextMiddleware` loads all of the caller's families and uses the one named, else the earliest joined. A family the caller doesn't belong to is ignored, so the header can only choose among their own families. Everything after that (query filters, `FamilyPolicy`, use cases, SignalR groups) is unchanged from ADR 0007.
- **Switching** is a select in the header (shown with two or more families). It stores the id in `localStorage` and reloads the page, which refreshes every screen and rejoins the new family's live-update group in one step.
- **Adding people.** An Owner can still create a brand new account for someone. For an existing account, the Owner sends an invite link, which the person opens while signed in. Nobody is added to a family without their own account's action.

## Consequences
- Anyone who can reach the API can create an account. Abuse is limited by the rate limit; email confirmation and CAPTCHA are not built. A duplicate-email error does reveal that an address is registered, which was accepted for a simpler sign-up.
- A person who is a Member in one family can still own others, which is the behaviour asked for.
- A saved active family that no longer applies falls back to the earliest joined family without an error.
- Migrations were reset again (synthetic data): delete `src/ExpenseTracker.Api/App_Data/` once after pulling.
- Not built: leaving a family you are the last Owner of is still refused (transfer ownership or add another Owner first), and there is no way to delete a whole family.
