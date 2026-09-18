# Design System Mode Routing

Use this reference when a request spans multiple Design System layers or repositories.

## GitHub Project cross-skill gate

When the request is represented by a GitHub Issue, apply the GitHub Project Workflow handoff
before choosing an implementation mode. If the Issue or request mentions `Design System`,
`design tokens`, `visual rules`, `theme`, `component contract`, `shared CSS`, `accessibility
contract`, `conformance`, or a new Design System component, check for a current approved Component
Specification first. If it is missing, use `design` and do not route directly to `build`.

`Issue Ready` is Requirement Approval only. It is not Design System Approval. Design System
Approval requires the confirmed Experience Brief, approved Component Specification, recorded
Accessibility requirements, and approved Test/Conformance Specification with completed mapping.
The build gate also requires an implementation plan and validation targets. When the GitHub
Project Workflow skill is active, use its canonical
`workflow/skills/github-project-workflow/references/design-system-handoff.md` reference for the
metadata values, lifecycle gate, mixed-Issue examples, and status rules.

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

Run `scripts/resolve_design_system_context.py <target-root> --mode design --format json` before
asking questions. Ask at most three unresolved questions about the experience, layout, actions,
states, or feedback. Do not ask for token names, CSS variables, component IDs, APIs, ARIA
attributes, DOM structure, package versions, or test runners. Produce and repeat a short
Experience Brief in plain language, and wait for confirmation before creating runtime
implementation, tokens, CSS, a Blazor component, an implementation branch, or an implementation
PR.

Design output should be a decision or contract proposal, not unreviewed framework code.

## Build mode

Use `build` only when shared artifacts belong in a Design System project and the complete handoff
gate is open. Read the confirmed Experience Brief, approved Component Contract, recorded
Accessibility requirements, mapped Test/Conformance scenarios, token source, implementation plan,
and validation targets before changing implementation code. Keep React and Blazor framework idioms
native, but preserve the same user-observable behavior and conformance IDs. Passing tests or an
Issue's `Ready` Status does not open the Design System gate.

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

For a mixed request, finish `design` before `build`. If the design and implementation are
independently reviewable or too large, recommend native parent/sub-issues rather than a custom
Project Status. Keep the GitHub lifecycle values unchanged.
