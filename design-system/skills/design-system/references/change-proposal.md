# Design System Change Proposal

Use this template when a consumer need may require a shared token, component contract, framework
implementation, or cross-host behavior change.

Do not start with a proposed prop or DOM shape. Start with the user and product need.

```markdown
# Change: <short title>

## Proposal

- Proposal ID: <stable id>
- Source project: <repository>
- Target Design System project: <repository>
- Requester: <team or person>
- Design System ID: <id>
- Current contract version: <version>
- Affected hosts: React / Blazor / both

## User and product need

<What a product user or application developer needs to accomplish.>

## Evidence

- Affected products:
- Frequency or recurrence:
- Current workaround:
- Accessibility or compliance impact:
- Why application-level composition is insufficient:

## Classification

- [ ] Existing component composition
- [ ] Consumer-local extension
- [ ] Token change
- [ ] Component contract change
- [ ] New component
- [ ] Framework-specific defect

## Contract impact

- API:
- Behavior and state:
- Accessibility:
- DOM / CSS observables:
- SSR / hydration / render modes:
- Backward compatibility:

## Consumer outcome

- Outcome: <resolved-in-consumer / consumer-local-extension / blocked-on-token-change / blocked-on-component-contract / blocked-on-package-upgrade / blocked-on-host-support / conformance-required>
- Blocking layer:
- Required upstream changes:
- Consumer work that can continue now:
- Resume condition:

## Acceptance scenarios

<Stable scenario IDs and Given / When / Then expectations.>

## Proposed ownership

- Specification owner:
- Token owner:
- React implementer:
- Blazor implementer:
- Conformance reviewer:

## Decision

<Approved, rejected, defer, or solve in the consumer. Include the reason.>
```

## Promotion rule

Promote a consumer solution into the shared Design System only when its semantics are reusable,
its API is understandable outside the originating product, and both supported hosts can satisfy
the contract or have an explicitly accepted deviation.

Do not promote a component merely because it was implemented twice. Repetition is evidence for
investigation, not automatic proof that the abstraction is correct.
