# Host Implementation Strategy

The default is shared contract plus framework-native implementation. Reuse should be introduced
only where it reduces a measured consistency or maintenance risk without making lifecycle,
rendering, typing, SSR, or accessibility harder for a host.

## Decision table

| Situation | Preferred approach | Reason |
| --- | --- | --- |
| Tokens, themes, icons, reset, and stable visual primitives | Shared tokens / CSS / assets | They are declarative and have no framework lifecycle. |
| Button, link, text input, simple select, badge, and layout primitives | React and Blazor native implementations | Native forms, events, refs, validation, and SSR are clearer in each host. |
| Dialog, popover, menu, tabs, combobox, tooltip, and focus-sensitive overlays | Native components with shared scenarios; optionally a small behavior kernel | The contract is shared, but framework ownership of state and rendering is important. |
| Pure DOM algorithm with explicit mount/update/dispose, such as focus containment or roving tabindex | Shared TypeScript behavior is worth considering | It can remove subtle parity bugs if the API is DOM-based and the interop cost is accepted. |
| A component whose public product is deliberately an HTML custom element and must serve many hosts | Web Component, with thin host wrappers where needed | The DOM contract is the product boundary and framework integration is secondary. |
| Complex forms, virtualized data grids, rich editors, or render-heavy controls | Host-native implementation plus shared contract/tests | Framework rendering, forms, performance, and SSR differences usually dominate. |

## Shared TypeScript behavior gate

Extract Shared TypeScript only when all of these are true:

- the algorithm is host-neutral and can be expressed in DOM terms;
- state ownership remains in React or Blazor, or the ownership boundary is explicit;
- the module exposes `mount`, `update`, and `dispose` (or an equivalent cleanup contract);
- Blazor JS Interop calls are bounded and do not turn every render into a round trip;
- SSR/prerender/hydration behavior is defined;
- the shared module has its own unit tests and both hosts have lifecycle/integration tests;
- a versioned compatibility contract exists between the behavior module and host packages.

Do not extract a “universal component” merely to share code. A shared kernel that owns hidden
state, renders markup, or dispatches framework-shaped events tends to recreate a third framework
with the lifecycle problems this architecture is intended to avoid.

## Web Component gate

Choose Web Components when the following are part of the requirement:

- a stable custom-element / attribute / property / event contract is intentionally public;
- non-React and non-Blazor consumers need the same DOM component;
- shadow DOM or custom-element encapsulation is an accepted styling boundary;
- form association, SSR/hydration, event typing, and accessibility behavior have explicit answers;
- React and Blazor wrappers add ergonomics rather than hiding essential behavior.

Avoid Web Components as the default when controlled React state, Blazor forms/validation, SSR,
templated content, or framework-native composition is central. In those cases the wrapper can
become a permanent impedance layer and the DOM becomes an accidental second API.

## Required decision record

For every shared-core proposal record:

- component or behavior ID and the parity problem being solved;
- why separate host implementations are insufficient;
- lifecycle and state ownership boundary;
- SSR/hydration and JS Interop cost;
- public DOM/event/style surface;
- test and versioning plan;
- exit or rollback criteria if the shared core increases host friction.
