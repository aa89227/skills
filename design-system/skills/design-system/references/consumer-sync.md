# Consumer Synchronization Protocol

“Always synchronized” means the consumer workflow always knows which released contract it is using
and can report a newer candidate. It does not mean silently changing application dependencies.

## Two checks, two meanings

1. **Installed check** runs automatically at the start of every `consume` or `conformance` task.
   It reads the exact manifest resolved by the consumer package reference/lockfile and is the only
   source used for imports, component names, CSS entrypoints, and usage guidance.
2. **Update check** runs when `catalog.latestManifest` or a configured local/registry provider is
   available. It compares release and contract metadata and reports `current`,
   `candidate-available`, `not-found`, `ambiguous`, or `different-system`.

The Skill performs these checks internally through the preflight resolver. A consumer developer
does not need to remember an inspection command. The command-line scripts are maintainer/debug
interfaces and CI building blocks.

## Recommended consumer context

```json
{
  "role": "consumer",
  "designSystemId": "company",
  "hosts": ["react", "blazor"],
  "packages": {
    "react": "@company/design-system-react",
    "blazor": "Company.DesignSystem.Blazor"
  },
  "catalog": {
    "manifest": "node_modules/@company/design-system-contracts/design-system.manifest.json",
    "latestManifest": ".design-system/registry/latest/design-system.manifest.json",
    "approvedVersion": "4.2.0"
  }
}
```

`manifest` is pinned to the installed release. `latestManifest` is informational and may be
updated by a package bot, release pipeline, or registry adapter. Neither field grants permission
to edit package manifests or lockfiles.

## Outcome rules

- Installed component and host mapping found: `resolved-in-consumer`.
- Component absent from installed release but present in the latest candidate:
  `blocked-on-package-upgrade`.
- Component absent from both installed and latest catalogs: `blocked-on-component-contract`.
- Installed catalog missing: `blocked-on-catalog`; do not guess an import or CSS path.
- Different system ID in the update source: `different-system` update status; never treat it as an
  upgrade.
- New behavior, token, or accessibility requirement that cannot be composed locally:
  return the matching upstream outcome and create a change proposal.

## CI and dependency bots

For continuous visibility, run the same read-only preflight in consumer CI on pull requests and on
a scheduled cadence. A bot may open an upgrade pull request containing the package version, manifest
diff, migration notes, and conformance/integration results. Merging that pull request remains an
explicit dependency change owned by the consumer team.

This separation prevents a newer Design System release from changing a build while an agent is
merely answering a usage question, while still making drift visible without manual inspection.
