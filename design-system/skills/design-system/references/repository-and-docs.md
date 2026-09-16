# Repository, Package, and Documentation Layout

Keep the Skill Catalog, the Design System source, and product consumers as separate repositories
or clearly separate projects. A monorepo is useful for atomic Design System releases; it is not a
reason to make consumers depend on source directories.

## Recommended Design System monorepo

```text
design-system/
├── tokens/                         # canonical token source and generated distributions
├── specs/components/<id>/          # Component Specification and contract version
├── packages/
│   ├── contracts/                  # manifest, contract references, scenario metadata
│   ├── css/                        # shared CSS and theme entrypoints
│   ├── react/                      # React-native components
│   ├── blazor/                     # Blazor / C# components
│   └── behavior/                   # optional, only approved TS behavior kernels
├── conformance/
│   ├── scenarios/                  # shared host-neutral scenarios
│   ├── react/                      # React adapters and reports
│   └── blazor/                     # Blazor adapters and reports
├── docs/                           # framework-neutral contract and usage guidance
├── examples/react/                 # React showcase fixtures
├── examples/blazor/                # Blazor showcase fixtures
└── scripts/                        # token, manifest, docs, and release checks
```

The exact build tooling may differ. The ownership boundaries should not: tokens and contracts are
upstream, CSS is shared, host packages implement the contract, and conformance consumes both host
packages.

## Package boundaries and versions

- Publish CSS, contracts/manifest, React, and Blazor as independently consumable packages, even if
  they are released by one monorepo pipeline.
- Keep a contract version separate from package versions. A patch package may fix an implementation
  without changing the contract; a contract change must trigger both host review gates.
- Put the manifest in the contracts/catalog artifact and include a validated copy or resolvable
  reference in each host release. The manifest must identify the exact CSS, contract, docs, and
  host package versions used by that release.
- Consumers resolve the installed package first. Latest/approved versions are update information;
  they must not silently change lockfiles or dependencies.
- A release is publishable only after token/CSS checks, both host builds, accessibility checks,
  shared conformance scenarios, documentation/example checks, and manifest validation pass.

## Documentation model for React and Blazor

Use one documentation site as the information architecture and two host-specific execution paths:

- **Framework-neutral pages:** purpose, anatomy, states, behavior, accessibility, CSS/token
  contract, compatibility, and host-neutral scenarios.
- **React pages/examples:** imports, props, controlled state, events, refs, SSR, and React fixtures.
- **Blazor pages/examples:** package registration, Razor parameters, `EventCallback`, render modes,
  forms/validation, disposal, and Blazor fixtures.

Storybook is a good React interactive renderer and a useful documentation shell, but it should not
be treated as the sole source of truth for Blazor. Use a Blazor showcase/test app for Blazor
examples and link both renderers to the same contract pages and scenario IDs. If a single docs site
embeds both, keep the examples physically separate and label the host explicitly.

## CI ownership gates

```text
contract/token lint
    -> CSS and host package builds
    -> React + Blazor shared conformance
    -> accessibility and visual checks
    -> docs/examples and manifest validation
    -> versioned publish
```

Consumer repositories should run installed-package integration checks and report a change proposal
when the package cannot satisfy a reusable need. They should not import `specs/` or host source code
directly from the Design System repository.
