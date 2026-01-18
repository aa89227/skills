# Skills

A personal collection of Claude Code skill plugins.

> **⚠️ Personal Use Only**
> This repository is maintained for my own use. If you find something useful, feel free to fork it. I make no guarantees about updates, maintenance, or breaking changes.

## About

This repository contains reusable skill plugins for [Claude Code](https://claude.ai/code), providing specialized knowledge and workflows for various development tasks.

## Available Plugins

### 🔷 C# Best Practices
**Location:** `csharp-best-practices/`

C# 14 best practices skill plugin with dotnet CLI automation:
- Modern C# language features (C# 12/13/14)
- Best practices and patterns
- Dotnet CLI agent for build/test/publish operations
- High-density code examples

### 🔷 Git Operations
**Location:** `git-operations/`

Git workflow skill plugins:
- Commit message conventions
- Git worktree usage guide

## Installation

1. Clone this repository to your local machine
2. Follow Claude Code plugin installation instructions
3. Configure individual plugins as needed

## Structure

Each plugin follows this convention:
```
plugin-name/
├── .claude-plugin/
│   └── plugin.json      # Plugin manifest
├── agents/              # Optional: specialized agents
├── skills/              # Skill definitions
│   └── skill-name/
│       └── SKILL.md     # Skill content
└── ...
```

## Requirements

- [Claude Code](https://claude.ai/code)
- Familiarity with Claude Code plugin system

## License

MIT License - see [LICENSE](LICENSE) file for details.

## Disclaimer

These plugins are provided "as is" without warranty of any kind. Use at your own risk.
