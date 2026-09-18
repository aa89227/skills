# User-facing Experience Intake

Use this as the front door when a person describes a Design System need in product or visual
language. The requester does not need to know Design Tokens, Component APIs, ARIA, DOM contracts,
React, Blazor, or package versions.

## Conversation boundary

Ask about the experience, not the implementation:

- what the person is trying to do and when the UI appears;
- what it should look like: placement, size, emphasis, hierarchy, theme, density, and motion;
- how it should be used: trigger, primary actions, navigation, dismissal, confirmation, and feedback;
- how it should look and behave while loading, unavailable, empty, invalid, successful, or failed;
- a screenshot, design reference, or natural-language example when one exists.

Do not ask the requester to choose:

- whether to use the Design System or which existing component to use; inspect and compose the
  available system first;
- a token name, CSS variable, component ID, prop/parameter name, or DOM structure;
- controlled versus uncontrolled state, ARIA attributes, focus-trap implementation, or test runner;
- React versus Blazor behavior when the Design System declares both hosts;
- a package name or installed version. Resolve those from project files and package manifests.

When this intake is reached from a GitHub Issue, it is the required first step whenever the Issue
or request involves Design System work but no current approved Component Specification can be
verified. An Issue `Ready` status is only Requirement Approval; it does not authorize skipping
this intake or starting `build`.

Accessibility, responsive behavior, keyboard support, reduced motion, and semantic HTML are Design
System quality defaults. Ask about them only when the product experience itself is ambiguous, not as
an implementation quiz.

## Progressive questions

Ask only for decisions that cannot be inferred from the request, existing design references, or the
current Design System. Run
`scripts/resolve_design_system_context.py <target-root> --mode design --format json` before
asking. Ask at most three short questions in one turn, and do not repeat a decision that is already
known. Prefer a concrete visual or interaction question over a technical multiple-choice question.

Good questions:

1. “這個畫面是要幫使用者完成什麼事情？什麼時候出現？”
2. “它希望看起來比較像置中的視窗、側邊面板，還是頁面上的區塊？有參考畫面嗎？”
3. “使用者要怎麼打開、完成、取消和離開？載入或失敗時希望看到什麼？”

Do not ask all possible questions by default. If the requester has supplied a design, screenshot,
existing component, or detailed example, inspect it and ask only the unresolved experience decisions.
Reflect the interpretation back in plain language before committing to a shared Design System change.
The response must include a compact Experience Brief and a plain-language restatement for the
requester to confirm. Until that confirmation, do not create runtime implementation, tokens, CSS,
a Blazor component, an implementation branch, or an implementation PR. A brief confirmation is
not the complete Design System Approval; after confirmation, derive the internal Component,
Accessibility, and Test/Conformance Specifications and obtain their approvals separately.

## Internal translation

After the experience is sufficiently clear, the agent performs this translation without requiring
the requester to learn the internal vocabulary:

```text
visual language
  -> existing or new semantic tokens, themes, density, CSS variants

interaction language
  -> existing or new component, states, transitions, keyboard/focus behavior

edge-case descriptions
  -> accessibility requirements, conformance scenarios, error/loading states

host/package context
  -> React / Blazor mappings and installed-release compatibility from preflight
```

The agent should first try to satisfy the experience with existing tokens and component composition.
It should create a new token or component contract only when the described experience cannot be
represented by the existing system. The requester may see a plain-language summary and a proposed
visual/interaction result; technical artifacts are produced for maintainers and implementation
teams.

## Experience brief

Once clarified, record a compact brief in the user's language:

```text
Experience: <what the person needs to accomplish>
Visual: <what it looks like, where it appears, emphasis, theme, density, motion>
Interaction: <how it opens, changes, confirms, cancels, and closes>
States: <loading, unavailable, empty, invalid, success, failure, or other visible states>
References: <screenshots, designs, or examples>
```

The brief is input to the internal Component Specification and shared Test Specification. It is not
itself a replacement for those artifacts. The Design System contract is not ready for `build` until
the brief is confirmed, the Component Specification is approved, Accessibility requirements are
recorded, Test/Conformance scenarios are created and mapped, and an implementation plan with
validation targets has been produced.

## When the experience is not yet enough

If a shared change is likely but the visual or interaction intent is still unclear, pause and ask
for that experience decision. Do not fill the gap with a guessed token, API, DOM shape, or framework
behavior. If the experience is clear but the installed package cannot provide it, use the normal
consumer outcome and change-proposal flow; do not ask the requester to select an arbitrary package.
