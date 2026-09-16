# Design System Mode Routing

Use this reference when a request spans multiple Design System layers or repositories.

## Mode matrix

| Project role | `design` | `build` | `consume` | `conformance` | `release` |
| --- | --- | --- | --- | --- | --- |
| `skill-catalog` | Design Skill workflow | Update Skill artifacts | Explain Skill usage | Validate Skill structure | Publish Skill/plugin |
| `design-system` | Define or change system contract | Implement shared artifacts | Use local showcase only | Required for host parity | Publish shared packages |
| `consumer` | Submit a product or gap proposal | Implement app-local composition | Primary mode | Run integration checks | Upgrade consumed packages |
| `showcase` | Define example scenarios | Build demos/fixtures | Exercise published APIs | Host-specific evidence | Usually none |
| `conformance` | Define test coverage | Maintain harness | Not a product consumer | Primary mode | Publish reports |

The matrix is a boundary guide, not a permission to perform unrelated work. Follow the user's
explicit scope and authorization.

## Design mode

Use `design` to understand and decide:

- the requester-facing experience: what the person needs to see, do, and experience;
- whether the need belongs in the application or shared system;
- the internal token and component impact derived from that experience.

Start with [experience-intake.md](experience-intake.md) when the requester is not an engineer.
Ask about visual and interaction intent in plain language. The following technical decisions are
internal outputs, not questions the requester must answer:

- whether a need belongs in the application or shared system;
- token taxonomy and semantic naming;
- component API, state, behavior, accessibility, and DOM/CSS contracts;
- supported hosts and render modes;
- governance, ownership, and compatibility policy.

Design output should be a decision or contract proposal, not unreviewed framework code.

## Build mode

Use `build` only when shared artifacts belong in a Design System project. Read the approved
Component Contract and token source before changing implementation code. Keep React and Blazor
framework idioms native, but preserve the same user-observable behavior and conformance IDs.

If the active project is a consumer, build mode means application-local code unless the user
explicitly identifies a separate Design System target. A request for a shared change becomes a
change proposal instead of an accidental local fork.

## Consume mode

Use `consume` to:

1. Inspect the installed Design System package and documented contract.
2. Prefer existing components and composition patterns.
3. Keep product-specific behavior in the application layer.
4. Add integration tests using user-observable semantics.
5. Escalate repeated or cross-product gaps through a change proposal.

Do not solve a missing shared capability by copying internal component code into the consumer.

## Conformance mode

Use `conformance` when checking React and Blazor parity. Run the same scenario IDs against both
hosts where possible. Separate:

- shared user-observable scenarios;
- host-specific unit and lifecycle tests;
- accessibility automation;
- manual accessibility checks;
- visual regression checks.

Record intentional differences as explicit deviations with an owner, reason, affected versions,
and removal criteria.

## Cross-project handoff

When moving from a consumer request to a Design System change, carry forward:

- proposal ID;
- source and target project roots;
- Design System ID and contract version;
- affected hosts;
- current workaround;
- proposed token or component impact;
- acceptance and conformance scenarios;
- next owner and validation gate.

The receiving workflow must re-resolve target context. It must not rely on the previous Skill's
working directory or conversation assumptions.
