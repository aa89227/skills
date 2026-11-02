---
name: ask
description: |
  Answer a specific question from available context and, when needed, read-only investigation.
  Use when the user wants a direct explanation, cause, location, behavior, or yes/no answer
  without code changes or unsolicited implementation advice, including explicit ask, explain,
  question, 請問, or 解釋 requests.
license: MIT
metadata:
  author: aa89227
  version: "1.2"
---

# Ask Mode

## Skill Execution Contract

Use this fixed loop:

1. Identify the single question and its time scope.
2. Use the available context first, then perform only the read-only inspection needed to answer it.
3. Separate verified facts, repository evidence, inference, and uncertainty.
4. Lead with the answer in the user's language and stop when the question is answered.

`ask` MUST remain read-only. It MUST NOT turn a question about current behavior into a requirement
decision, implementation proposal, Issue update, or broader design discussion. If the user asks
whether behavior or an external contract should change, route the discussion to `workflow:discuss`.

### Answer output contract

Use this English structure as the stable output template. The actual response MAY localize the
headings and prose to the user's language.

```text
## Answer

## Evidence

## Limitations or Uncertainty
```

The final answer is complete when it directly answers the question, cites the relevant evidence when
available, states material uncertainty, and has performed no mutation.

Correct example:

```text
Yes. The current implementation reads the value from the repository configuration.
Evidence: `path/to/config` defines the setting and `path/to/test` verifies the fallback.
```

Incorrect example:

```text
The current behavior is probably fine; I recommend redesigning it with a new configuration model.
```

## Instruction Retention

Read this file when this skill is first activated for the current task. Once read, retain and
follow its instructions without rereading the file on every turn or before every action.

Reread it only when the file may have changed, the current context no longer contains its
instructions (for example after context compaction or a new session), the instructions are
ambiguous or conflicting, or exact wording must be verified.

Answer the user's specific question without changing state.

## Workflow

1. Use the available conversation and repository context first.
2. Perform only the read-only inspection needed to resolve missing facts.
3. Lead with the answer, then provide concise supporting evidence.
4. Cite relevant files and line numbers when the answer depends on source code.
5. Clearly separate verified facts from inference.
6. State what cannot be determined instead of speculating.

Do not create, edit, move, or delete files. Do not run commands that change repository, system,
service, or external state.

Stay focused on the question. Do not propose fixes, improvements, alternative designs, or broader
trade-offs unless the user explicitly asks for them.

For a yes/no question, begin with yes or no when the evidence supports a definitive answer.
