# GitHub Pages Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add a GitHub Pages site advertising the Sygnia application, with a download link for the design manual docx (from the docx plan), and update CLAUDE.md/SOLUTION.md to reflect that GitHub Pages is back in scope (reversing the prior "Out" decision, per explicit user confirmation).

**Architecture:** A static single-page site under `docs/gh-pages/` (or `/docs` root, whichever GitHub Pages source setting is used), built with plain HTML/CSS — no framework needed for a single advertising page — published via the repo's existing GitHub Pages settings or a dedicated workflow.

**Tech Stack:** Static HTML/CSS, GitHub Actions (`actions/deploy-pages`) or classic `docs/` branch-based Pages.

**Spec:** `docs/project-scaffold-done.md` Modifications items 11-12; user confirmed reversing the CLAUDE.md "Out" decision on 2026-08-16.

## Global Constraints

- This reverses an explicit prior decision recorded in `docs/SOLUTION.md`'s "Deliberate scope omissions" and root `CLAUDE.md`'s "Out" list — Task 1 updates both documents so the scope record stays accurate, per CLAUDE.md's own rule that scope changes get recorded.
- The manual's download link must point at a file the site actually serves — do not link to a path that doesn't exist in the published site.
- Depends on `docs/Sygnia-Design-Manual.docx` existing (see the design-manual-docx plan) — sequence this plan after that one, or stub the link and fill it in once the manual lands.

---

### Task 1: Update scope records in CLAUDE.md and SOLUTION.md

**Files:**
- Modify: `CLAUDE.md` (root) — the "Out" bullet under "Scope: what is in and what was deliberately cut"
- Modify: `docs/SOLUTION.md` — the "Deliberate scope omissions" section

**Interfaces:** None — documentation only.

- [ ] **Step 1: Edit CLAUDE.md**

Change:
```
- **Out:** Redis, Swagger, GitHub Pages. Each is recorded in `SOLUTION.md` as a deliberate omission.
```
to:
```
- **Out:** Redis, Swagger. Each is recorded in `SOLUTION.md` as a deliberate omission.
- **In (added 2026-08-16):** GitHub Pages, advertising the application and hosting the design manual for download — originally cut, reinstated per explicit user decision. See `SOLUTION.md`.
```

- [ ] **Step 2: Edit SOLUTION.md**

Change the "Redis, Swagger, GitHub Pages" bullet under "Deliberate scope omissions" to drop GitHub Pages, and add a new bullet or subsection noting it was reinstated on 2026-08-16, what it hosts (an advertising landing page + the design manual download), and why (explicit user request overriding the earlier cut).

- [ ] **Step 3: Commit**

```bash
git add CLAUDE.md docs/SOLUTION.md
git commit -m "docs: reinstate GitHub Pages in scope, update scope records"
```

---

### Task 2: Build the landing page

**Files:**
- Create: `docs/index.html`
- Create: `docs/styles.css`

**Interfaces:**
- Consumes: content from `docs/first_draft.md` / `docs/SOLUTION.md` for the description copy
- Produces: a static page GitHub Pages will serve from `docs/` (GitHub's own "Pages source: `/docs` on `main`" setting)

- [ ] **Step 1: Write the page content** — project name, one-paragraph description (what Sygnia does and why it exists as a portfolio piece), tech stack badges/list, a link to the GitHub repo, and a prominent "Download design manual" button.

- [ ] **Step 2: Write `docs/index.html`**

```html
<!DOCTYPE html>
<html lang="en">
<head>
  <meta charset="UTF-8">
  <title>Sygnia</title>
  <link rel="stylesheet" href="styles.css">
</head>
<body>
  <main>
    <h1>Sygnia</h1>
    <p class="tagline">A gRPC-first ledger service demonstrating idempotent writes and streaming reads.</p>
    <section>
      <h2>Tech stack</h2>
      <ul>
        <li>.NET 8 / gRPC / EF Core / SQL Server</li>
        <li>Angular 18 (gRPC-Web)</li>
        <li>Serilog + Seq, OpenTelemetry + Jaeger</li>
      </ul>
    </section>
    <a class="cta" href="Sygnia-Design-Manual.docx" download>Download the design manual</a>
    <a class="repo-link" href="https://github.com/<owner>/Sygnia">View on GitHub</a>
  </main>
</body>
</html>
```

(Replace `<owner>` with the actual GitHub org/user once known — check `git remote get-url origin`.)

- [ ] **Step 3: Write a minimal `docs/styles.css`** for readable typography and a clear call-to-action button — no framework needed for one page.

- [ ] **Step 4: Copy the manual into `docs/`**

Run: `cp docs/Sygnia-Design-Manual.docx docs/Sygnia-Design-Manual.docx` — already in place if the design-manual plan ran first and output directly to `docs/`; otherwise copy it there so the relative download link in Step 2 resolves.

- [ ] **Step 5: Open `docs/index.html` locally in a browser** and verify layout, and that the download link/href resolves to a real file on disk.

- [ ] **Step 6: Commit**

```bash
git add docs/index.html docs/styles.css
git commit -m "feat: add GitHub Pages landing site with design manual download"
```

---

### Task 3: Enable GitHub Pages

**Files:**
- Modify (only if using Actions-based Pages instead of the classic `/docs` folder setting): `.github/workflows/pages.yml`

**Interfaces:** None — repository configuration.

- [ ] **Step 1: Check current Pages configuration**

Run: `gh api repos/{owner}/{repo}/pages 2>&1 || echo "not configured"`
Expected: tells you whether Pages is already enabled and which source it uses.

- [ ] **Step 2: Enable Pages from the `/docs` folder on `main`** (simplest option, no workflow needed, matches the plain-static-site approach in Task 2)

This is a GitHub repo setting change (Settings → Pages → source: Deploy from a branch → `main` / `/docs`) — **confirm with the user before changing repository settings**, since this is a shared/visible-to-others change per the "actions visible to others" guidance.

- [ ] **Step 3: Verify the published site**

Run: `gh api repos/{owner}/{repo}/pages --jq .html_url` once enabled, then fetch that URL and confirm the landing page and download link both work.

---

## Self-review notes

- Task 1 is required before Task 2/3 so the scope-record contradiction the user flagged doesn't persist in CLAUDE.md while the feature ships.
- Task 3 explicitly calls out that enabling Pages is a repo-settings change requiring user confirmation, per the "actions visible to others" guidance in this environment — this plan does not assume blanket authorization to flip that switch.
- This plan assumes the design-manual-docx plan has already produced `docs/Sygnia-Design-Manual.docx`; if run out of order, Task 2 Step 4 will have nothing to copy — sequence accordingly or stub the link.
