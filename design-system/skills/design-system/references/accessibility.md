# Accessibility Gate

Accessibility is part of the Component Specification, not a final visual review. React and Blazor
implementations may use different APIs, but the resulting semantic tree and keyboard/focus
experience must satisfy the same contract.

## Contract requirements

For every interactive component, specify:

- native element and ARIA pattern, with the reason if native semantics are insufficient;
- accessible name source and precedence;
- description and error relationships;
- state/property mapping (`aria-expanded`, `aria-selected`, `aria-disabled`, `aria-busy`, and so
  on) and when each attribute is present or absent;
- keyboard commands, focus target, focus containment, focus restoration, and behavior when the
  original target was removed;
- announcement behavior for asynchronous updates, validation, and status changes;
- reduced motion, forced colors, zoom/reflow, touch target, and contrast expectations;
- ID ownership and relationship guarantees across rerender, SSR, hydration, and multiple instances.

Use established WAI-ARIA patterns as a reference, but specify the actual product behavior and
prefer native HTML semantics whenever they provide the required behavior.

## Host implementation rules

- React and Blazor must emit equivalent roles, names, states, relationships, and keyboard outcomes.
- Never use `aria-*` as a substitute for a missing native behavior. For example, `aria-disabled`
  does not itself prevent activation.
- Generated IDs must be deterministic within an instance, collision-safe across instances, and
  stable across a server/client transition where the render model requires it.
- A component must clean up document listeners, focus guards, scroll locks, portals, and live-region
  nodes during close and unmount/disposal.
- Do not use framework-generated class names as accessibility hooks. Use semantic elements and the
  stable attributes declared by the contract.
- Accessibility fixes that alter a role, keyboard path, focus target, or public state are contract
  changes and require both host implementations plus conformance scenarios.

## Evidence required for a release

For each release-blocking interactive component, retain:

1. shared keyboard/focus scenarios passing for React and Blazor;
2. automated accessibility scan results for representative states;
3. explicit assertions for accessible name, role, state, and relationships;
4. manual keyboard checks for flows that automation cannot reliably prove;
5. a deviation record for every intentional host difference.

An automated scan with no violations is necessary but insufficient: it does not prove Escape
dismissal, focus trap, restoration, event ordering, or correct announcements.
