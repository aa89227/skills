# Consumer Outcome

Every `consume` request ends with an explicit outcome. The outcome is part of the Skill's response
contract; it is not an instruction for the user to run a discovery command.

## Outcomes

| Outcome | Meaning | Consumer action |
| --- | --- | --- |
| `resolved-in-consumer` | The installed catalog and host API satisfy the request | Continue in the consumer |
| `consumer-local-extension` | The need is product-specific and does not belong in the shared system | Implement locally with an explicit boundary |
| `blocked-on-catalog` | The installed Design System catalog cannot be resolved | Resolve package/provider context before usage guidance |
| `blocked-on-token-change` | The shared visual language is insufficient | Change token source, then rebuild packages |
| `blocked-on-component-contract` | Shared behavior/API/accessibility is insufficient | Change the Component Specification first |
| `blocked-on-package-upgrade` | The capability exists only in another compatible release | Request or perform an explicit dependency upgrade |
| `blocked-on-host-support` | The component or behavior is not available for the active host | Add host support or choose a supported composition |
| `conformance-required` | React and Blazor behavior must be compared before proceeding | Run the shared conformance scenarios |
| `blocked-on-context` | Project, host, or target repository is ambiguous | Resolve the target without mutating artifacts |

## Required response shape

When the request cannot be completed entirely in the consumer, report:

```text
Outcome: blocked-on-component-contract
Current project: consumer
Design System: company
Installed release: 4.2.0
Affected hosts: react, blazor
Blocking layer: Component Specification

Required upstream changes:
  1. Update the component contract
  2. Implement React and Blazor behavior
  3. Add conformance scenarios
  4. Release a compatible package

Consumer work that can continue now:
  Prepare the integration boundary and acceptance fixture.

Resume condition:
  The released package exposes the requested contract version.
```

Do not claim that a component is absent until the exact installed catalog has been inspected. If a
latest catalog candidate is available, report it as an upgrade candidate without changing the
Consumer dependency.

## Decision boundaries

- Missing documentation is not automatically a missing component.
- A one-off product need is not automatically a shared component request.
- A component present in the catalog but missing for the active host is a host-support outcome,
  not a new component outcome.
- A behavior mismatch between React and Blazor is a conformance or implementation outcome before
  it becomes a new API proposal.
