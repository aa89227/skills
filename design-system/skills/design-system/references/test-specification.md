# Shared Test Specification

React and Blazor should share the behavioral scenario definitions, not necessarily the test runner
or assertion library. The shared test specification is the executable interpretation of the
Component Specification and is a release input.

## Handoff gate

For a new or changed shared component, create the Test/Conformance scenarios and complete their
mapping to the Component and Accessibility Specifications before `build`. The mapping is part of
Design System Approval; passing tests without a recorded scenario mapping, an `Issue Ready` status,
or an Agent's own judgment cannot open the implementation gate. Scenario IDs and host adapters are
internal outputs, not choices the requester must make.

## Scenario shape

Each scenario should have the following. A machine-readable envelope is available in
[conformance-scenarios.schema.json](conformance-scenarios.schema.json), with a concrete example in
[conformance-scenarios.example.json](conformance-scenarios.example.json):

- a stable ID: `<component>.<state-or-flow>.<behavior>`, for example
  `dialog.open.focus-containment`;
- a short intent and the contract rule it covers;
- host-neutral setup, action, and observable assertions in Given / When / Then form;
- supported hosts and any explicit deviations;
- a priority and release gate;
- optional accessibility, DOM/CSS, timing, or visual assertion categories.

Example:

```yaml
id: dialog.close.focus-restoration
component: dialog
priority: release-blocking
hosts: [react, blazor]
given:
  - a trigger button is focused
  - the dialog is closed
when:
  - the user opens the dialog
  - the user presses Escape
then:
  - the dialog is no longer modal and visible
  - focus is restored to the trigger button when it remains connected
assertions:
  accessibility:
    - the dialog has no stale aria-modal=true state after close
  timing:
    - focus restoration occurs at the contract-defined close boundary
```

The host adapters translate “trigger button”, “presses Escape”, and “focus” into Playwright,
Testing Library, bUnit, or another runner. They must not weaken the assertion to a framework
implementation detail.

## Test layers

Use the same scenario ID at every applicable layer:

1. **Contract scenarios** — shared JSON/YAML and Given / When / Then semantics. This is the parity
   source of truth.
2. **Host adapter tests** — React Testing Library / Playwright and bUnit / Playwright setup,
   framework lifecycle, parameter binding, event translation, and disposal.
3. **Accessibility checks** — automated axe-style checks plus explicit keyboard/focus assertions.
   Automated scans are a supplement; they do not prove focus behavior or correct accessible names.
4. **DOM/CSS checks** — only for markers declared stable by the contract: semantic elements, ARIA,
   data-state/part markers, token-backed computed styles, and reduced-motion behavior.
5. **Visual regression** — host-specific screenshots for layout and theme coverage. A screenshot
   must not replace semantic or keyboard assertions.

## Parity rules

- A release is conformant only when every `release-blocking` scenario passes for every supported
  host, or has an explicit deviation record.
- Run React and Blazor with the same scenario IDs and equivalent fixture data.
- Compare user-observable outcomes, not internal state names, generated IDs, framework event
  objects, or exact DOM nesting that the contract does not declare.
- A deviation records the scenario ID, host, affected versions, reason, user impact, owner,
  temporary workaround, and removal condition. It cannot be silently encoded as a second contract.
- If a behavior cannot be expressed host-neutrally, first review the Component Specification. It
  may be a host-specific concern, or the contract may be underspecified.

## Minimum component coverage

For an interactive component, the scenario set should cover as applicable:

- default, disabled, loading, invalid, readonly, and empty states;
- primary pointer and keyboard paths;
- focus entry, movement, containment, exit, and restoration;
- accessible name, role, state, relationship, and announcement behavior;
- controlled/uncontrolled updates and rerendering;
- SSR/prerender/hydration or interactive render-mode boundaries;
- cleanup on close, unmount, disposal, and navigation;
- reduced motion, forced colors, themes, and responsive/density variants;
- event ordering and cancellation when the API exposes events.

## Release gate

The Design System release pipeline should produce a report keyed by component ID, scenario ID,
host, package version, contract version, and result. A green host-specific test suite without the
shared scenario ID mapping is not evidence of React / Blazor parity.
