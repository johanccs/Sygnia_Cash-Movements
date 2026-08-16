# Code Quality & Cleanup Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Bring the existing Sygnia.Backend/Sygnia.Frontend/Sygnia.WpfClient codebase in line with the Modifications items 1-3 and 4-7 in `docs/project-scaffold-done.md`: concise comments, no over-engineering, no stray files, SRP, meaningful names, methods ≤15 lines.

**Architecture:** Pure refactor pass, no behavior change. Work project by project so `dotnet build && dotnet test` (backend) and `ng build` (frontend) stay green after every task.

**Tech Stack:** .NET 8, Angular 18, existing test suite (`Sygnia.Tests`).

**Spec:** `docs/project-scaffold-done.md` (Modifications 1-7), root `CLAUDE.md` coding standards §"Coding standards".

## Global Constraints

- No behavior change — every task ends with `dotnet build && dotnet test` green (backend) or `ng build` green (frontend).
- Methods must not exceed 15 lines (root CLAUDE.md coding standard).
- Comments: replace long/narrative comments with short ones that explain *why*, not *what* — delete comments that just restate the code.
- Never touch `main` directly; this plan assumes a feature branch already exists (see gh-branch-workflow / CLAUDE.md workflow step 1).

---

### Task 1: Remove stray untracked files

**Files:**
- Delete: `src/Sygnia.Frontend/image.png`, `image-1.png` … `image-5.png`, `user-page.png`
- Delete (if confirmed unused): `src/Sygnia.WpfClient/Sygnia.WpfClient.sln`, `src/Sygnia.WpfClient/.github/` (only if these duplicate the root solution/workflow and are not referenced anywhere)

**Interfaces:** None — pure deletion task.

- [ ] **Step 1: Confirm nothing references the PNGs**

Run: `grep -rn "image.png\|image-1.png\|image-2.png\|image-3.png\|image-4.png\|image-5.png\|user-page.png" src/Sygnia.Frontend/src docs 2>/dev/null`
Expected: no matches (they are leftover screenshots dropped in the project root, not asset references).

- [ ] **Step 2: Confirm the WpfClient `.sln`/`.github` are not the canonical build entry point**

Run: `grep -rln "Sygnia.WpfClient.sln" .github/ 2>/dev/null` and check whether `Sygnia.Backend.sln` or a root solution already covers `Sygnia.WpfClient`.
Expected: if `Sygnia.WpfClient` builds fine via the existing backend solution/CI, the nested `.sln` and `.github/` are redundant scaffolding and safe to delete. If CI actually depends on them, keep them and skip this half of the task.

- [ ] **Step 3: Delete confirmed-unused files**

```bash
git rm src/Sygnia.Frontend/image.png src/Sygnia.Frontend/image-1.png src/Sygnia.Frontend/image-2.png src/Sygnia.Frontend/image-3.png src/Sygnia.Frontend/image-4.png src/Sygnia.Frontend/image-5.png src/Sygnia.Frontend/user-page.png
```
(add the WpfClient `.sln`/`.github` removal only if Step 2 confirmed they're redundant)

- [ ] **Step 4: Verify builds still pass**

Run: `dotnet build src/Sygnia.Backend/Sygnia.Backend.sln` and `ng build` from `src/Sygnia.Frontend`.
Expected: both succeed unchanged.

- [ ] **Step 5: Commit**

```bash
git commit -m "chore: remove stray screenshot files and redundant WpfClient scaffolding"
```

---

### Task 2: Comment audit — Sygnia.Domain

**Files:**
- Modify: all `.cs` files under `src/Sygnia.Backend/src/Sygnia.Domain/`

**Interfaces:** None — comment-only changes, no signature changes.

- [ ] **Step 1: List all multi-line or narrative comments**

Run: `grep -rn "///\|//" src/Sygnia.Backend/src/Sygnia.Domain --include=*.cs | grep -v "^\S*:\s*//\s*$"`
Review each hit: keep only comments explaining a non-obvious *why* (e.g. guard-clause ordering, invariant rationale); delete or shorten the rest to one line.

- [ ] **Step 2: Apply edits file by file**, keeping XML-doc summaries on public members but trimming them to one sentence where they currently run multiple paragraphs.

- [ ] **Step 3: Build and run domain unit tests**

Run: `dotnet test src/Sygnia.Backend --filter FullyQualifiedName~Domain`
Expected: PASS, unchanged test count.

- [ ] **Step 4: Commit**

```bash
git commit -m "docs: trim Sygnia.Domain comments to concise why-notes"
```

---

### Task 3: Comment audit — Sygnia.Application, Sygnia.Infrastructure, Sygnia.Presentation

**Files:**
- Modify: `.cs` files under `src/Sygnia.Backend/src/Sygnia.Application/`, `.../Sygnia.Infrastructure/`, `.../Sygnia.Presentation/`

**Interfaces:** None — comment-only changes.

- [ ] **Step 1: Repeat the audit from Task 2 Step 1** scoped to these three projects.

- [ ] **Step 2: Apply edits**, paying particular attention to the idempotency (2627/2601 catch) and streaming (`AsAsyncEnumerable`) code paths — CLAUDE.md calls these the two invariants, so their *why* comments should stay, just tightened.

- [ ] **Step 3: Full backend test run**

Run: `dotnet build src/Sygnia.Backend/Sygnia.Backend.sln && dotnet test src/Sygnia.Backend`
Expected: PASS, same pass/fail counts as before this task.

- [ ] **Step 4: Commit**

```bash
git commit -m "docs: trim Application/Infrastructure/Presentation comments"
```

---

### Task 4: Comment audit — Sygnia.Frontend and Sygnia.WpfClient

**Files:**
- Modify: `.ts`/`.html` under `src/Sygnia.Frontend/src/`, `.cs`/`.xaml.cs` under `src/Sygnia.WpfClient/`

**Interfaces:** None.

- [ ] **Step 1: Audit and trim comments** in both projects the same way as Tasks 2-3.

- [ ] **Step 2: Verify builds**

Run: `ng build` (from `src/Sygnia.Frontend`) and `dotnet build src/Sygnia.WpfClient/Sygnia.WpfClient.csproj` (or via the WCF/WPF solution if it covers it).
Expected: both succeed.

- [ ] **Step 3: Commit**

```bash
git commit -m "docs: trim Frontend and WpfClient comments"
```

---

### Task 5: Complexity pass — find and simplify over-engineered logic

**Files:** any file flagged in Step 1

**Interfaces:** Depends on findings; any signature change must be reflected in all callers within the same task.

- [ ] **Step 1: Run the code-review skill at medium effort against the current branch diff and the pre-existing codebase**

Use `/code-review medium` (or invoke the `code-review` skill directly) scoped to look specifically for unnecessary abstraction, unused indirection, or speculative generality — not correctness bugs (that's a separate pass).

- [ ] **Step 2: For each finding, simplify** — inline unnecessary interfaces/wrappers that have exactly one implementation and no test-seam reason to exist, collapse unnecessary layers.

- [ ] **Step 3: Re-run full test suite after each simplification**

Run: `dotnet test src/Sygnia.Backend`
Expected: PASS after every individual simplification (commit between them, don't batch).

- [ ] **Step 4: Commit each simplification separately**

```bash
git commit -m "refactor: simplify <specific area>"
```

---

### Task 6: SRP / naming / method-length pass

**Files:** any `.cs` method exceeding 15 lines, or any class/method whose name doesn't describe a single responsibility

**Interfaces:** Extracted private methods must have descriptive names; no public interface changes unless a class is genuinely doing two jobs (rare — flag for user confirmation before splitting a public type).

- [ ] **Step 1: Find methods over 15 lines**

Run (PowerShell, from repo root):
```powershell
Get-ChildItem -Recurse -Filter *.cs src/Sygnia.Backend/src | ForEach-Object {
  $lines = Get-Content $_.FullName
  # manual scan: any method body between opening/closing brace exceeding 15 non-blank lines
}
```
In practice: open each file under `src/Sygnia.Backend/src/*/` and visually scan for methods that run past 15 lines — there's no lint rule wired up yet, so this is a manual read.

- [ ] **Step 2: Extract sub-steps into well-named private methods** so each method reads as one responsibility. Verify each class still has one clear reason to change (SRP) — if a class mixes e.g. validation and persistence, flag it and confirm the split with the user rather than restructuring silently.

- [ ] **Step 3: Rename anything unclear** (e.g. generic `Handle`, `Process`, `DoWork`) to describe what it actually does.

- [ ] **Step 4: Run full test suite after each class's changes**

Run: `dotnet test src/Sygnia.Backend`
Expected: PASS, unchanged behavior.

- [ ] **Step 5: Commit per class/file**

```bash
git commit -m "refactor: extract <method> from <class> to keep methods under 15 lines"
```

---

## Self-review notes

- Tasks 2-4 cover every project named in root CLAUDE.md's architecture tree — no project skipped.
- Task 1 only deletes files after an explicit "is this referenced" check, per CLAUDE.md's guidance to investigate before deleting unfamiliar files.
- Task 6 explicitly stops and asks before splitting a public class, since that changes the public interface CLAUDE.md's "one Add&lt;Layer&gt;()" rule depends on.
