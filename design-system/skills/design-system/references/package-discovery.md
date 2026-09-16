# Design System Package Discovery

Use this reference in `consume`, `conformance`, and `release` workflows. The Skill remains stable;
the released Design System package supplies the versioned catalog that changes with each release.

## Package manifest responsibility

`design-system.manifest.json` is a generated release artifact. It is not the canonical source for
behavior; the Component Specification remains authoritative. The manifest makes the released
system discoverable to Skills, documentation, test harnesses, and consumer tooling.

The manifest should describe:

- Design System identity and release version;
- contract version and optional contract fingerprint;
- supported hosts;
- CSS package and entrypoint;
- component IDs, display names, statuses, aliases, and replacements;
- component contract and documentation references;
- React exports and Blazor types;
- host-specific examples;
- package compatibility information.

Validate the artifact against [design-system.manifest.schema.json](design-system.manifest.schema.json).
Use [design-system.manifest.example.json](design-system.manifest.example.json) as the smallest
concrete example.

## Discovery order

Resolve the exact installed catalog in this order:

1. `catalog.manifest` from `.design-system/project.json`.
2. A manifest explicitly supplied by the user or consuming tool.
3. A standard manifest in the target project:
   - `.design-system/design-system.manifest.json`;
   - `design-system.manifest.json`;
   - a directly referenced package under `node_modules`.
4. A package-specific adapter for a NuGet or private registry layout.

If more than one candidate is found and no explicit path selects one, report the ambiguity. Do not
choose the newest filename or directory by guesswork.

The Skill should invoke the inspector internally during its automatic preflight. Maintainers can
run it directly when debugging a provider or validating a release artifact:

```bash
python3 scripts/inspect_design_system_manifest.py <target-root> --format text
```

When a local registry snapshot or approved latest manifest is available, compare it without
changing dependencies:

```bash
python3 scripts/inspect_design_system_manifest.py <target-root> \
  --latest-manifest <latest-manifest-path> --format text
```

The inspector is read-only. It reports `not-found`, `ambiguous`, `found`, or
`component-not-found`; an unresolved catalog is not evidence that the component does not exist.
The user-facing workflow should report the resulting context and outcome, not ask the user to run
the inspector command.

The repository provider also checks local `obj/project.assets.json` files for manifests bundled in
resolved NuGet package folders. It does not restore packages or contact NuGet. For a private feed,
configure an explicit `catalog.manifest` after restore, or point `catalog.registry` at a local
registry snapshot for update discovery. A registry URL is metadata for a future provider and is not
treated as evidence of an installed package.

## Version semantics

Keep these values separate:

- **Installed release:** the manifest resolved from the package version in the lockfile or project
  reference. Use this to generate code and usage guidance.
- **Latest published release:** an optional registry or `latestManifest` result. Use this only to
  report an update.
- **Latest compatible release:** a release satisfying the consumer's supported contract and host
  constraints.
- **Approved release:** the version allowed by the organization's consumer policy.

Never update a consumer dependency merely because a newer manifest is discoverable. If the installed
release is older, report the difference and keep the generated usage compatible with the installed
release unless the user explicitly requests an upgrade.

## Component resolution

Resolve a component in this order:

1. Stable `component.id`.
2. A declared alias.
3. Display name, only when it maps to exactly one component.

Use `component.id` in conformance scenarios, proposals, and cross-host comparisons. Treat React
exports, Blazor type names, and display names as host or presentation mappings, not as the stable
identity.

When a component is found, select the host mapping for the active project:

```text
component.id: dialog
host: react
package: @company/design-system-react
export: Dialog
contract: components/dialog/contract.json
example: examples/dialog.react.tsx
```

If the component exists in the catalog but not for the active host, report a host support gap. If
the component is only present in a newer catalog, report an upgrade candidate. If it is absent from
the latest available catalog, create a change proposal rather than inventing a consumer API.

## Release checks

Before publishing a new host package:

- generate the manifest from the same Component Specifications used by the build;
- include both host mappings when both hosts are supported;
- include the CSS entrypoint and contract version;
- ensure deprecated components declare a replacement when one exists;
- run conformance against the release candidate;
- verify React and Blazor package versions resolve to the intended contract;
- publish versioned docs or references that match the manifest.
