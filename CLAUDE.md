# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

這是一個 Claude Code skill plugins 的集合倉庫，包含可重用的技能插件。

## Architecture

```
skills/
├── .claude-plugin/
│   └── marketplace.json     # 插件索引，列出所有可用插件
├── csharp-best-practices/   # C# 14 最佳實踐插件
│   ├── .claude-plugin/
│   │   └── plugin.json      # 插件清單
│   └── skills/
│       └── csharp-best-practices/
│           ├── SKILL.md     # 主要技能檔（範例與規則）
│           └── BEST-PRACTICES.md  # 維護說明與原理
└── git-operations/          # Git 工作流插件
    ├── .claude-plugin/
    │   └── plugin.json
    └── skills/
        ├── git-commit-messages/
        │   └── SKILL.md     # 提交訊息規範
        └── git-worktrees/
            └── SKILL.md     # worktree 使用指南
```

## Plugin Structure Convention

每個插件遵循相同結構：
- `plugin.json`: 插件名稱、版本、作者
- `skills/[skill-name]/SKILL.md`: 技能定義（YAML frontmatter + markdown）

SKILL.md frontmatter 必須包含：
```yaml
---
name: skill-name
description: 觸發描述
license: MIT
metadata:
  author: aa89227
  version: "1.0"
  tags: ["tag1", "tag2"]
---
```

## Content Guidelines

- `SKILL.md`: 高密度、可直接複製使用的範例與規則
- `BEST-PRACTICES.md`: 原理、陷阱、相容性說明（不重複 SKILL.md 內容）

## MCP Configuration

`opencode.json` 配置了 Microsoft Learn MCP 伺服器，可使用 `use microsoft-learn` 啟用文件查詢工具。
