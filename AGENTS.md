# AGENTS.md

This repository publishes the same reusable skills through Claude Code and Codex plugin formats, and exposes the plugin skill directories as a Pi coding agent package.

## Project Overview

The repository contains reusable skill plugins for C#/.NET development, Git operations, and agent workflow support. Each plugin exposes skills through both supported hosts when both manifests are present.

## Skill Source of Truth and Edit Scope

This repository is the source of truth for the skills it publishes. When a task concerns a skill provided by this repository, inspect and modify the matching files under this repository's plugin directories, especially `*/skills/*/SKILL.md`, rather than an installed copy.

Treat user-level skill directories and installed skill copies (for example, under `~/.codex/skills` or `~/.claude/skills`) as read-only runtime dependencies. Do not edit, delete, overwrite, or otherwise mutate them for a repository task. Before writing a skill file, verify that its path is inside this repository; if a user-level copy was loaded or reported by a tool, locate the corresponding repository file and make the change there. Do not modify files outside this repository unless the user explicitly asks for that separate change.

## Skill Instruction Retention

When a repository-local skill is activated for the current task, read its `SKILL.md` once, retain
and follow its instructions, and do not reread it on every turn or before every action. If the
runtime has already provided the skill content, use that loaded content instead of issuing
another read.

Reread the file only when it may have changed, the current context no longer contains its
instructions (for example after context compaction or a new session), the instructions are
ambiguous or conflicting, or exact wording must be verified.

## Repository Layout

~~~
skills/
├── package.json             # Pi package manifest
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
├── workflow/
│   ├── .claude-plugin/plugin.json
│   ├── .codex-plugin/plugin.json
│   └── skills/[skill-name]/SKILL.md
└── design-system/
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
| Pi coding agent | package.json (root) | Pi package manifest; `pi.skills` exposes plugin skill roots |

Keep the paired manifests aligned for the plugin name, supported skill set, and release version. A plugin version change must be applied to both manifests.

## Pi Package Manifest

The root `package.json` is the Pi package entrypoint. Its `pi.skills` array must list each plugin's `skills/` directory so `pi install git:github.com/aa89227/skills` exposes the same source-of-truth skills. Keep paths relative to the repository root and update the array when a plugin skill root is added, removed, or renamed. Do not duplicate skills under `.pi/skills` or `.agents/skills`.

Pi installs this repository as a Git package. It reads the root `package.json` `pi` manifest; it does not use the `version` fields in `.claude-plugin/plugin.json` or `.codex-plugin/plugin.json` to resolve the package. A root `package.json` `version` is not required for this Git install, so do not add a synthetic aggregate version solely for Pi. Keep plugin versions synchronized only for their Claude Code and Codex manifests.

`pi install git:github.com/aa89227/skills` is intentionally branch-based and not reproducibly pinned. To pin a Git tag or commit, use `pi install git:github.com/aa89227/skills@<tag-or-commit>`; Pi updates a pinned package without moving it to another ref. The unqualified source may move when the repository's default branch is updated. Pi writes personal installs to its agent settings by default; `-l`/`--local` opts into a project-local `.pi/settings.json`. Do not add `.pi/settings.json` merely to make this repository installable.

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
5. Keep the root `package.json` `pi.skills` array aligned with plugin skill roots when adding, removing, or renaming a plugin.

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
