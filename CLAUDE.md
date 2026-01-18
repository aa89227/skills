# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

This is a collection repository of Claude Code skill plugins containing reusable skill plugins.

## Architecture

```
skills/
├── .claude-plugin/
│   └── marketplace.json     # Plugin index listing all available plugins
├── csharp-best-practices/   # C# 14 best practices plugin
│   ├── .claude-plugin/
│   │   └── plugin.json      # Plugin manifest
│   └── skills/
│       └── csharp-best-practices/
│           ├── SKILL.md     # Main skill file (examples and rules)
│           └── BEST-PRACTICES.md  # Maintenance notes and rationale
└── git-operations/          # Git workflow plugin
    ├── .claude-plugin/
    │   └── plugin.json
    └── skills/
        ├── git-commit-messages/
        │   └── SKILL.md     # Commit message conventions
        └── git-worktrees/
            └── SKILL.md     # Worktree usage guide
```

## Plugin Structure Convention

Each plugin follows the same structure:
- `plugin.json`: Plugin name, version, author
- `skills/[skill-name]/SKILL.md`: Skill definition (YAML frontmatter + markdown)

SKILL.md frontmatter must include:
```yaml
---
name: skill-name
description: Trigger description
license: MIT
metadata:
  author: aa89227
  version: "1.0"
  tags: ["tag1", "tag2"]
---
```

## Content Guidelines

- `SKILL.md`: High-density, copy-paste ready examples and rules
- `BEST-PRACTICES.md`: Rationale, pitfalls, compatibility notes (no duplication with SKILL.md)

## MCP Configuration

`opencode.json` configures the Microsoft Learn MCP server. Use `use microsoft-learn` to enable documentation query tools.
