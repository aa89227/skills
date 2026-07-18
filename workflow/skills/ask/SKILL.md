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
  version: "1.1"
---

# Ask Mode

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
