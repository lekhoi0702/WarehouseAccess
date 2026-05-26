# CODEX.md

Behavioral guidelines to reduce common LLM coding mistakes. Merge with project-specific instructions as needed.

**Tradeoff:** These guidelines bias toward caution over speed. For trivial tasks, use judgment.

## 0. Collaboration And Edit Gate

**Default workflow is analysis-first. Do not edit immediately.**

Before touching files:

* First do: analyze issue -> propose concrete code fix.
* Do **not** create/edit files yet unless user explicitly approves.
* Must ask user for approval before any file create/edit action.
* Only after user replies with clear approval (e.g. "ok") may implementation begin.

Additional mandatory rule:
* When user asks to fix/modify code, always present a concrete proposed solution first.
* Only apply code changes after user explicitly approves that proposal.

## 0.1 Data Contract Accuracy (No Guessing)

**Code must match real backend contracts.**

When implementing FE <-> BE integration:

* Verify exact BE endpoint, method, request shape, and response shape from actual backend code.
* Return/map data in FE exactly according to real response contract.
* Do not normalize by assumption.
* Do not add fallback behavior based on guesses.
* If contract is unclear or inconsistent, stop and ask user before coding.

## 0.2 Reusability, Duplication, And Simplicity Priority

**Prefer reusable and simple code over page-specific or over-engineered code.**

Before writing code, always analyze:

* Can this logic be reused across pages/modules?
* Is there existing shared code that should be extended instead of duplicated?
* Will this change create one-off code for only one page without necessity?

Rules:

* Do not write code that only serves exactly one page if a shared/reusable approach is reasonable.
* Avoid duplicated logic; extract shared functions/services when practical.
* Prioritize simplicity and correctness over advanced/clever techniques.
* Choose the most straightforward implementation that is accurate and maintainable.

## 0.3 Frontend Reuse First

**Frontend is always a high-priority reuse area.**

For FE changes, always perform careful analysis first and then propose:

* Reuse strategy for CSS (shared classes, common styles, avoid page-only style duplication).
* Reuse strategy for JS (shared utility functions, shared module logic, avoid copy-paste handlers).
* Impact scope across related pages before introducing new FE code.

Rules:

* FE code should default to reusable/shared patterns.
* Do not add isolated one-page FE code when shared implementation is possible.
* Always include FE reuse analysis in the proposed solution before implementation.

## 0.4 Reuse And Duplication Check For Every Request (BE + FE)

**For every request, always perform reuse/duplication review first.**

Mandatory analysis before coding:

* Check whether the target code is duplicated.
* Check whether the logic can be reused from existing BE/FE code.
* Check whether new code would create unnecessary duplication.

Mandatory proposal output:

* Explicitly state duplication findings (if any).
* Explicitly state reuse opportunities for both backend and frontend.
* Propose a reuse-first fix path before any implementation starts.

## 0.5 Multi-User Scope Control

**This project is collaborative; implementation scope must be strictly permission-based.**

Rules:

* Do not assume full-apply across all related files/modules.
* Apply only the exact parts that the user explicitly allows.
* If broader changes are beneficial, propose them separately first and wait for approval.
* Never expand scope silently in a multi-user codebase.

## 0.6 Naming Rules (Variables/Functions)

**Use names that are practical, clear, correct, and concise.**

Rules:

* Prefer real-world meaning over vague names (avoid unclear names like `data1`, `tmp2`).
* Keep names short but still descriptive enough to understand intent quickly.
* Follow the project naming style consistently (camelCase in JS/TS, existing conventions in file scope).
* Do not over-abbreviate; only use abbreviations that are common and unambiguous (`id`, `url`, `api`).
* If a better name improves readability, refactor the name as part of the change.

## 1\. Think Before Coding

**Don't assume. Don't hide confusion. Surface tradeoffs.**

Before implementing:

* State your assumptions explicitly. If uncertain, ask.
* If multiple interpretations exist, present them - don't pick silently.
* If a simpler approach exists, say so. Push back when warranted.
* If something is unclear, stop. Name what's confusing. Ask.

## 2\. Simplicity First

**Minimum code that solves the problem. Nothing speculative.**

* No features beyond what was asked.
* No abstractions for single-use code.
* No "flexibility" or "configurability" that wasn't requested.
* No error handling for impossible scenarios.
* If you write 200 lines and it could be 50, rewrite it.

Ask yourself: "Would a senior engineer say this is overcomplicated?" If yes, simplify.

## 3\. Surgical Changes

**Touch only what you must. Clean up only your own mess.**

When editing existing code:

* Don't "improve" adjacent code, comments, or formatting.
* Don't refactor things that aren't broken.
* Match existing style, even if you'd do it differently.
* If you notice unrelated dead code, mention it - don't delete it.

When your changes create orphans:

* Remove imports/variables/functions that YOUR changes made unused.
* Don't remove pre-existing dead code unless asked.

The test: Every changed line should trace directly to the user's request.

## 4\. Goal-Driven Execution

**Define success criteria. Loop until verified.**

Transform tasks into verifiable goals:

* "Add validation" → "Write tests for invalid inputs, then make them pass"
* "Fix the bug" → "Write a test that reproduces it, then make it pass"
* "Refactor X" → "Ensure tests pass before and after"

For multi-step tasks, state a brief plan:

```
1. \[Step] → verify: \[check]
2. \[Step] → verify: \[check]
3. \[Step] → verify: \[check]
```

Strong success criteria let you loop independently. Weak criteria ("make it work") require constant clarification.

\---

**These guidelines are working if:** fewer unnecessary changes in diffs, fewer rewrites due to overcomplication, and clarifying questions come before implementation rather than after mistakes.

