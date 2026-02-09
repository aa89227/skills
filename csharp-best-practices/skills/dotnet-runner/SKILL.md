---
name: dotnet-runner
description: Specialized .NET CLI handler that provides enhanced error recovery and output parsing for dotnet commands. Use when: (1) Building .NET projects, (2) Running tests, (3) Publishing applications, (4) Managing NuGet packages, or (5) Any task involving dotnet build, dotnet test, dotnet run, or dotnet publish commands.
license: MIT
metadata:
  author: aa89227
  version: "1.2"
  tags: ["dotnet", "cli", "build", "test", "automation"]
  trigger_keywords: ["build", "test", "run", "publish", "pack", "restore", "dotnet"]
---

# dotnet-runner Skill

## When to Invoke the dotnet-runner Agent

**Always invoke the `dotnet-runner` agent** (via `Task` tool with `subagent_type: "csharp-best-practices:dotnet-runner"`) when:

1. **User explicitly requests CLI operations:**
   - "build the project"
   - "run tests"
   - "publish the app"
   - "restore dependencies"
   - "pack the library"
   - "run the application"

2. **After code changes that require validation:**
   - Completed implementing a feature → invoke agent to build
   - Fixed compilation errors → invoke agent to verify build
   - Updated dependencies → invoke agent to restore and test

3. **Multi-step workflows:**
   - "Build and test" → invoke agent once with both operations
   - "Restore, build, and run" → invoke agent to chain commands
   - "Publish to Release" → invoke agent with publish configuration

## Command Quick Reference

| Operation | Agent Handles | Common Scenarios |
|-----------|---------------|------------------|
| `build` | Compilation | After code changes, verifying fixes |
| `test` | Unit/integration tests | After implementation, CI/CD validation |
| `run` | Execute application | Local testing, debugging |
| `publish` | Create deployment artifacts | Release preparation |
| `pack` | Create NuGet packages | Library distribution |
| `restore` | Restore dependencies | After updating `.csproj` |

## Agent Invocation Pattern

```markdown
Use the Task tool to invoke dotnet-runner agent:
- subagent_type: "csharp-best-practices:dotnet-runner"
- prompt: Brief description of what to do (e.g., "build the project", "run tests")
```

## Usage Examples

### Example 1: After Implementing a Feature
```
Scenario: User asks "Add a new UserService class with CRUD methods"
Action: Implement the class → Invoke dotnet-runner to build and test
Reason: Verify compilation and ensure tests pass
```

### Example 2: User Requests Explicit Build
```
Scenario: User says "build the solution"
Action: Immediately invoke dotnet-runner with "build the project"
Reason: Direct user request for CLI operation
```

### Example 3: Multi-step Validation
```
Scenario: User says "Fix the test failures and make sure everything works"
Action: Fix code → Invoke dotnet-runner to run tests
Reason: Validate fixes before marking task complete
```

### Example 4: Release Workflow
```
Scenario: User says "publish the API to Production"
Action: Invoke dotnet-runner with "publish to Release configuration"
Reason: Generate production-ready artifacts
```

## Key Principles

- **Proactive Invocation:** Don't ask "should I build?" — invoke the agent when appropriate
- **Single Responsibility:** The agent handles CLI execution; you handle code analysis and planning
- **Clear Communication:** Tell the agent exactly what operation to perform
- **Error Handling:** If agent reports failures, analyze output and fix issues

## What the Agent Does

- Executes `dotnet` CLI commands (build, test, run, publish, pack, restore)
- Returns concise success/failure status
- Provides error messages when operations fail
- **Does not** read or analyze source code

## What You Do

- Decide **when** to invoke the agent based on context
- Analyze source code and plan implementations
- Interpret agent results and fix issues
- Communicate progress to user

## Notes

- This skill focuses on **decision guidance** (when to invoke), not CLI documentation
- For .NET CLI reference, see official docs or use `microsoft-learn` MCP tools
- The agent is designed for execution; you are responsible for strategic decisions
