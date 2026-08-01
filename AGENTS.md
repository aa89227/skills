# AGENTS.md

This repository publishes the same reusable skills through two plugin formats: Claude Code and Codex.

## Project Overview

The repository contains reusable skill plugins for C#/.NET development, Git operations, and agent workflow support. Each plugin exposes skills through both supported hosts when both manifests are present.

## Repository Layout

~~~
skills/
├── .claude-plugin/
│   └── marketplace.json       # Claude Code marketplace index
├── .agents/
│   └── plugins/
│       └── marketplace.json   # Codex marketplace index
├── csharp-best-practices/
│   ├── .claude-plugin/plugin.json
│   ├── .codex-plugin/plugin.json
│   └── skills/[skill-name]/SKILL.md
├── git-operations/
│   ├── .claude-plugin/plugin.json
│   ├── .codex-plugin/plugin.json
│   └── skills/[skill-name]/SKILL.md
└── workflow/
    ├── .claude-plugin/plugin.json
    ├── .codex-plugin/plugin.json
    └── skills/[skill-name]/SKILL.md
~~~

## Plugin Formats

Every plugin directory that supports both hosts keeps a paired manifest:

| Host | Manifest | Purpose |
| --- | --- | --- |
| Claude Code | .claude-plugin/plugin.json | Claude Code plugin metadata and version |
| Codex | .codex-plugin/plugin.json | Codex plugin metadata and version |

Keep the paired manifests aligned for the plugin name, supported skill set, and release version. A plugin version change must be applied to both manifests.

## Marketplace Indexes

The repository has one marketplace index for each host:

- Claude Code: .claude-plugin/marketplace.json
- Codex: .agents/plugins/marketplace.json

Both indexes should expose the same set of plugin directories. When a plugin is added, removed, renamed, or republished, update both marketplace files and verify that their source paths still point to the repository's plugin directories.

## Change Checklist

When changing a plugin:

1. Update both .claude-plugin/plugin.json and .codex-plugin/plugin.json when both manifests exist.
2. Keep their versions synchronized.
3. Update both marketplace indexes if the plugin set or source path changes.
4. Validate the edited JSON files and the affected skill structure.

## Skill Format

Each SKILL.md uses YAML frontmatter with at least:

~~~
---
name: skill-name
description: Trigger description
license: MIT
metadata:
  author: aa89227
  version: "1.0"
  tags: ["tag1", "tag2"]
---
~~~

Keep skill content high-density and copy-paste ready. Put rationale, pitfalls, and compatibility notes in a nearby reference or maintenance document rather than duplicating them in SKILL.md.

## Documentation Tooling

opencode.json configures the Microsoft Learn MCP server. Use use microsoft-learn when documentation queries are needed.

Claude Code-specific entry-point guidance is in [CLAUDE.md](CLAUDE.md); this file remains the canonical source for shared repository instructions.
