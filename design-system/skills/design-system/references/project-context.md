# Design System Project Context

Resolve project context before selecting a Design System workflow. Project context answers
**where the work belongs**; task mode answers **what the user wants to do**. Keep those decisions
separate.

## Explicit manifest

The preferred declaration is `.design-system/project.json` at the target repository root. Validate
it against [project-context.schema.json](project-context.schema.json).

Design System project:

```json
{
  "$schema": "./.design-system/project-context.schema.json",
  "role": "design-system",
  "designSystemId": "company",
  "hosts": ["react", "blazor"],
  "sourceOfTruth": {
    "tokens": "tokens",
    "specifications": "specs",
    "styles": "styles",
    "conformance": "tests/conformance"
  }
}
```

Consumer project:

```json
{
  "$schema": "./.design-system/project-context.schema.json",
  "role": "consumer",
  "designSystemId": "company",
  "hosts": ["react"],
  "packages": {
    "react": "@company/design-system-react"
  },
  "catalog": {
    "manifest": "node_modules/@company/design-system-contracts/design-system.manifest.json"
  }
}
```

Skill Catalog project:

```json
{
  "$schema": "./.design-system/project-context.schema.json",
  "role": "skill-catalog",
  "provides": ["design-system"]
}
```

The project manifest is metadata, not a replacement for the actual source of truth. Its paths should
point to real project-owned artifacts, and package names should match the host that consumes them.
`catalog.manifest` is the preferred explicit path to the exact installed Design System manifest.
`catalog.latestManifest` or `catalog.registry` may be used for update discovery, but neither should
silently change the Consumer dependency.

## Fallback detection

When the manifest is missing, gather read-only evidence in this order:

1. A Design System source layout containing token sources, component specifications, host packages,
   and conformance tests.
2. A consumer package reference to a released React or Blazor Design System package without the
   corresponding source directories.
3. Plugin manifests, `skills/`, and `SKILL.md` files indicating a Skill Catalog.
4. The requested task and its target path.

Do not classify a repository from one directory name alone. A consumer may contain a local
showcase, and a Design System repository may contain example applications.

## Multiple target projects

The current working directory is not necessarily the target project. This is common when the Skill
Catalog is used to work on a separate Design System repository or consumer repository.

If more than one repository is in scope, identify them explicitly:

```text
skillProject: /path/to/skills
designSystemProject: /path/to/design-system
consumerProject: /path/to/product
activeTarget: consumerProject
```

Never infer a sibling directory as the Design System target solely because its name looks plausible.

## Required context summary

Before a mutating step, report:

```text
Project role: consumer
Task mode: consume
Design System: company
Hosts: react
Target root: /path/to/product
Evidence: explicit manifest and React package reference
Allowed artifacts: application code, integration tests, usage documentation
Next gate: integration test or change proposal if the package is insufficient
```

If the evidence conflicts, preserve the conflict in the summary and stay read-only until the target
is resolved.
