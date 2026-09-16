---
name: design-system
description: |
  Design, create, evolve, review, or consume a multi-framework Design System when the work spans
  tokens, component contracts, React and Blazor libraries, accessibility, conformance, or the
  handoff between a Design System repository and a consuming application.
license: MIT
metadata:
  author: aa89227
  version: "0.3"
  tags: ["design-system", "design-tokens", "react", "blazor", "accessibility", "conformance"]
---

# Design System

Use this as the entry point for Design System work. It routes the request by two independent
dimensions:

- **Project role:** what the current target repository owns.
- **Task mode:** what the user is asking to do now.

Do not treat a Skill repository, a Design System repository, and a consumer application as the
same project. A Skill is a capability that operates on Design System artifacts; it is not a token,
component, or runtime package.

## Audience boundary

The person describing a Design System need may be a product owner, designer, domain expert, or
ordinary user. Do not assume that person knows Design Tokens, component APIs, ARIA, DOM/CSS
contracts, React, Blazor, or package management.

For a design request, ask only about the experience in plain language: what it should look like,
when it appears, how it is used, and how it responds in visible states. Then translate that brief
internally into tokens, components, accessibility requirements, test scenarios, and host
implementations. Read [experience-intake.md](references/experience-intake.md) for the question
boundary and progressive intake rules.

## Start every invocation with context resolution

1. Identify the target project. The target is the repository being inspected or changed, not the
   location where this Skill was installed.
2. Read `.design-system/project.json` when it exists. Follow
   [project-context.md](references/project-context.md) for the schema and fallback signals.
3. Classify the task mode: `design`, `build`, `consume`, `conformance`, or `release`.
4. Automatically run the read-only preflight
   `scripts/resolve_design_system_context.py <target-root> --mode <mode> --format json` for every
   Design System task. For `consume` and `conformance`, this preflight must resolve the installed
   package catalog before recommending a component, API, import, CSS entrypoint, usage example, or
   upgrade. For `release`, validate an explicitly supplied or generated candidate manifest when
   one exists; a source Design System does not need to have its own package installed. The user does
   not need to run this command.
5. When the mode is `design`, capture or clarify a plain-language experience brief before asking
   for technical artifacts. Ask at most three unresolved visual/interaction questions at a time;
   do not ask the requester to choose tokens, APIs, ARIA, DOM, package versions, or host mappings.
6. Read [package-discovery.md](references/package-discovery.md) for catalog/provider behavior and
   [consumer-outcome.md](references/consumer-outcome.md) when the preflight reports a gap or
   prerequisite.
7. State the resolved project role, task mode, Design System context, outcome, evidence, and
   allowed artifact scope before taking a repository-changing action.
8. If role, target, catalog, or host is ambiguous, inspect read-only evidence first. Do not modify
   shared Design System artifacts until the target is explicit.

The current repository is a Skill Catalog when the task concerns plugin manifests, `SKILL.md`,
Skill references, or Skill validation. In that role, modify Skill assets only; do not create
React / Blazor runtime components here unless the user explicitly changes the repository scope.

## Project roles

| Role | Owns | Default boundary |
| --- | --- | --- |
| `skill-catalog` | Plugins, Skills, references, templates, validation | Defines how Design System work is performed |
| `design-system` | Tokens, specifications, CSS, React / Blazor libraries, conformance | Owns shared behavior and published artifacts |
| `consumer` | Product code and integration tests | Uses released artifacts and submits change proposals |
| `showcase` | Live examples and documentation fixtures | Demonstrates a host without becoming the source of truth |
| `conformance` | Cross-host test harness and reports | Verifies implementations against the contract |

One person may fill several roles, but the artifact ownership boundaries still apply.

## Task modes

Use the smallest mode that satisfies the request:

- `design`: define principles, taxonomy, governance, or a component contract.
- `build`: implement or update tokens, CSS, React, Blazor, docs, or tests in a Design System
  project. Implementation must follow an approved contract; do not invent behavior in code.
- `consume`: select, compose, configure, or integrate released components in a consumer project.
- `conformance`: compare host implementations, accessibility behavior, DOM/CSS observables, and
  test scenarios.
- `release`: version, deprecate, publish, or prepare migration guidance for shared artifacts,
  including generation and validation of the package manifest.

In `design` mode, the requester-facing input is an Experience Requirement, not a Component
Specification. The agent should first understand visual and interaction intent, then derive the
technical specification internally. Existing tokens, components, package metadata, host mappings,
and accessibility defaults should be inspected or inferred automatically.

When a consumer needs a capability that is not provided, classify it before coding:

1. Existing component composition.
2. Consumer-local extension.
3. New or changed token.
4. New or changed component contract.
5. Framework-specific defect.

Only the last three normally require work in the Design System project. Use
[change-proposal.md](references/change-proposal.md) when a consumer request crosses that boundary.

## Consumer-to-Design-System feedback loop

When a consumer cannot satisfy a request with the installed package, keep the loop explicit:

1. Complete the automatic preflight and resolve the installed component by stable ID, host, and
   contract version.
2. Try composition first, then classify the gap as local extension, token change, component
   contract change, package upgrade, or host-specific defect.
3. If the gap is shared, stop before copying source or inventing a consumer API. Produce the
   structured outcome and a change proposal containing the use case, affected hosts, acceptance
   scenarios, current workaround, and resume condition.
4. Hand the proposal to the Design System target with source/target repositories and contract
   version recorded. The receiving workflow re-resolves its own project context.
5. After the Design System publishes a compatible package, rerun the same preflight and scenario
   IDs. Resume consumer implementation only when the recorded condition is true.

The loop is a handoff protocol, not a recursive mutation permission: a consumer task may identify
and prepare an upstream change, but it does not edit shared tokens, specifications, or host packages
unless the active target is explicitly changed to the Design System project.

## Mode gates and required references

Apply the narrowest gate that matches the task:

| Mode | Required gate | Output before implementation |
| --- | --- | --- |
| `design` | Experience brief, then internal Component / Accessibility / Test Specification | Plain-language experience summary and internal design decision |
| `build` | Approved Component Specification and mapped conformance scenarios | Host implementation plan and validation targets |
| `consume` | Installed package manifest, host API mapping, and usage docs | Resolved usage or explicit consumer outcome |
| `conformance` | Component Specification, shared scenarios, and Accessibility gate | Per-host parity report or deviation record |
| `release` | Repository/package layout, release checks, conformance report, and manifest | Publish/migration decision |

Read these references as needed:

- [experience-intake.md](references/experience-intake.md) for non-technical visual and interaction
  questions, progressive intake, and internal translation.
- [experience-requirement.schema.json](references/experience-requirement.schema.json) and
  [experience-requirement.example.json](references/experience-requirement.example.json) for the
  normalized experience brief.
- [component-specification.md](references/component-specification.md) for the normative component
  contract and DOM/CSS boundary.
- [test-specification.md](references/test-specification.md) for shared scenario IDs and host test
  adapters.
- [conformance-scenarios.schema.json](references/conformance-scenarios.schema.json) and
  [conformance-scenarios.example.json](references/conformance-scenarios.example.json) for the
  machine-readable cross-host scenario envelope.
- [accessibility.md](references/accessibility.md) for the cross-host accessibility release gate.
- [implementation-strategy.md](references/implementation-strategy.md) before introducing Shared
  TypeScript behavior or Web Components.
- [repository-and-docs.md](references/repository-and-docs.md) for package, monorepo, CI, Storybook,
  and Blazor showcase boundaries.

## Routing and handoff rules

Read [mode-routing.md](references/mode-routing.md) when the task involves more than one mode or
more than one repository.

- A consumer request must carry its use case, affected hosts, acceptance scenarios, and evidence;
  it must not directly redefine a shared component API.
- A Design System implementer may propose contract changes but must not silently change the
  normative behavior while implementing React or Blazor code.
- React and Blazor implementations may use different framework APIs and lifecycle techniques, but
  they must satisfy the same semantic contract and conformance scenarios.
- A context switch from consumer work to Design System work requires a durable handoff. Record the
  source project, target project, contract version, affected artifacts, and next validation gate.
- If the request is only about authoring this Skill or its plugin, remain in the `skill-catalog`
  role even if the Skill describes a UI Design System.

## Sources of truth

Keep these responsibilities separate:

- Tokens source: canonical visual values and semantic names.
- Component contract: API meaning, state transitions, accessibility, DOM/CSS observables, and
  supported render modes.
- Conformance scenarios: executable user-observable expectations for every supported host. Use the
  shared scenario IDs and host adapters described in [test-specification.md](references/test-specification.md).
- Package manifest: versioned catalog generated from the contract and shipped with the release;
  it describes available components, host APIs, styles, examples, and compatibility metadata.
- Framework packages: React and Blazor implementations of the contract.
- Consumer code: application composition and integration, never the shared contract.
- Skill instructions: the workflow and decision rules for operating on those artifacts.

When artifacts disagree, identify the conflict explicitly. Do not make the implementation the
implicit source of truth.

## References

- Read [project-context.md](references/project-context.md) for project-role detection and the
  required context summary.
- Read [project-context.schema.json](references/project-context.schema.json) when creating or
  validating a project manifest.
- Run [resolve_project_context.py](scripts/resolve_project_context.py) for deterministic,
  read-only role detection; it does not select task mode or modify files.
- Run [resolve_design_system_context.py](scripts/resolve_design_system_context.py) as the automatic
  preflight that combines project role, task mode, host, catalog, component, and consumer outcome.
- Read [design-system-context.schema.json](references/design-system-context.schema.json) when
  validating or consuming the preflight result.
- Read [provider-protocol.md](references/provider-protocol.md) when adding or selecting a package,
  registry, or repository-specific catalog provider.
- Read [package-discovery.md](references/package-discovery.md) for installed-package catalog
  discovery, version semantics, component identity, and usage resolution.
- Read [consumer-sync.md](references/consumer-sync.md) for automatic installed/latest checks,
  update outcomes, and CI/dependency-bot handoff.
- Read [design-system.manifest.schema.json](references/design-system.manifest.schema.json) when
  creating or validating a released package manifest.
- Read [design-system.manifest.example.json](references/design-system.manifest.example.json) for a
  concrete manifest shape.
- Run [inspect_design_system_manifest.py](scripts/inspect_design_system_manifest.py) to inspect a
  manifest without modifying the target project when maintaining or debugging the provider.
- Read [consumer-outcome.schema.json](references/consumer-outcome.schema.json) when producing or
  validating a structured consumer result.
- Read [consumer-outcome.md](references/consumer-outcome.md) when a request cannot be completed
  entirely in the current project.
- Read [mode-routing.md](references/mode-routing.md) when routing a request or crossing repositories.
- Read [change-proposal.md](references/change-proposal.md) when a consumer need may require a
  token, contract, component, or cross-host change.
