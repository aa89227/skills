# Component Specification

The Component Specification is the normative boundary between product requests and React / Blazor
implementation. It describes the user-observable contract; it does not prescribe a framework,
class name, component state library, or lifecycle API.

It is an internal Design System artifact. A non-technical requester should describe the desired
visual and interaction experience instead; the agent and Design System maintainers translate that
brief into this specification.

## Required contract sections

Every shared component should have one versioned contract with these sections:

1. **Identity and purpose**
   - Stable `id` in kebab-case, for example `dialog`.
   - User problem, scope, non-goals, maturity, and deprecation/replacement policy.
   - Anatomy: named regions or slots that consumers may use. Do not make incidental wrapper nodes
     part of the contract.
2. **API contract**
   - Inputs / parameters: name, type meaning, requiredness, default, accepted values, and whether
     the value is controlled by the consumer or managed internally.
   - Outputs / events: semantic name, payload meaning, dispatch timing, and whether cancellation is
     possible. Do not expose framework event types as the shared contract.
   - Content model: children, named slots, templates, render fragments, or equivalent host mapping.
   - ID and ref rules: which IDs may be supplied, which IDs are generated, and which references must
     remain valid after rerendering.
3. **State model**
   - Enumerate observable states such as `open`, `disabled`, `loading`, `invalid`, and `readonly`.
   - Define legal transitions and their triggers. Say what happens when a trigger conflicts with a
     disabled or loading state.
   - Define controlled, uncontrolled, and initial-value behavior separately. A React prop and a
     Blazor parameter may have different names, but they must represent the same state ownership.
4. **Behavior contract**
   - Pointer, keyboard, focus, scrolling, dismissal, positioning, async, and form behavior.
   - Render-mode constraints: SSR, prerendering, hydration, interactive server, and static output
     where applicable.
   - Timing guarantees: event ordering, animation completion, loading transitions, and cleanup.
   - Error behavior and recovery. Never leave focus, listeners, portals, or scroll locks behind on
     unmount/disposal.
5. **Accessibility contract**
   - Required semantic element or ARIA role, accessible name, descriptions, relationships, and
     state/property mapping.
   - Keyboard map, focus entry, focus containment, focus exit, focus restoration, and live-region
     announcements where relevant.
   - ID generation and duplicate-ID rules. See [accessibility.md](accessibility.md).
6. **DOM / CSS observables**
   - The minimum stable root/part markers, semantic elements, attributes, ARIA attributes, and
     `data-*` state markers that tests or styling need.
   - CSS custom properties and token names that are public. Prefer semantic attributes and parts
     over framework-generated class names.
   - Portal / overlay ownership, stacking context, and whether content is present while closed.
   - Explicitly list DOM that is implementation detail and may change.
7. **Styling and motion**
   - Token references, density/size variants, themes, forced-colors behavior, and minimum visual
     states.
   - Enter/exit motion, reduced-motion behavior, and whether an interaction waits for animation.
     Motion must not be the only indication of state.
8. **Conformance scenarios**
   - Stable scenario IDs, host-neutral Given / When / Then statements, and required assertions.
   - Map each normative behavior and accessibility rule to at least one scenario. See
     [test-specification.md](test-specification.md).
9. **Compatibility and ownership**
   - Contract version, breaking-change policy, migration path, specification owner, host owners,
     and the release gate.

## Normative language

Use `MUST` for interoperability and accessibility requirements, `SHOULD` for the default that a
host may deviate from only with a documented reason, and `MAY` for optional capability. Each
`SHOULD` deviation needs an owner, affected release, user impact, and removal condition. Avoid
words such as “behaves normally” or “looks correct”; turn them into observable assertions.

## Example: Dialog contract excerpt

```yaml
id: dialog
contractVersion: "3"
states:
  - open
  - disabled
behavior:
  dismissal:
    escape: closes
    overlay: closes unless dismissible=false
  focus:
    onOpen: first enabled focusable descendant, otherwise dialog container
    containment: modal dialog MUST trap focus while open
    onClose: restore the element that initiated opening when it is still connected
accessibility:
  role: dialog
  modal: true
  name: labelledby title id, or explicit aria-label
  description: describedby description id when supplied
dom:
  required:
    - semantic dialog container
    - aria-modal=true while modal
    - stable relationship between title and dialog IDs
  implementationDetail:
    - framework-generated class names
    - internal focus sentinels
conformance:
  - dialog.open.escape
  - dialog.open.overlay-dismiss
  - dialog.open.focus-entry
  - dialog.open.focus-containment
  - dialog.close.focus-restoration
```

This excerpt is intentionally host-neutral. React may expose `open` and `onOpenChange`; Blazor may
expose `Open` and `OpenChanged`. The mapping belongs in the package manifest and host API docs. The
state transitions, focus result, ARIA observables, and scenario IDs do not change.

## Review gates

Before implementation is approved, reviewers should be able to answer:

- Can the behavior be tested without knowing whether the host is React or Blazor?
- Is every public prop/parameter tied to a user need or a contract state?
- Are DOM details limited to the observables needed for styling, accessibility, and testing?
- Can both hosts implement the contract without a host-specific interpretation of a key behavior?
- Is the request actually a reusable contract, rather than a product-specific composition?

If any answer is no, keep the work in `design` mode and resolve the contract before editing host
implementation code.
