---
name: readonly
description: |
  Handle the user's requested analysis, inspection, review, or report without changing any state.
  Use when the user explicitly asks to look, check, scan, investigate, or respond read-only,
  including "只看不改", "不要修改", and requests answerable from already loaded context.
license: MIT
metadata:
  author: aa89227
  version: "1.1"
---

# Readonly Mode

Complete the requested task without changing repository, system, service, or external state.

## Workflow

1. Start with the context already available in the conversation.
2. Perform additional read-only inspection only when the request requires evidence that is missing.
3. Follow the scope and output format requested by the user.
4. Report neutral observations when no problem is present.
5. When problems are present, report the evidence and plausible causes.
6. Label possible causes as inference unless they are directly verified.
7. State material uncertainty or unavailable evidence explicitly.

Do not force an inventory, file-by-file survey, severity list, or other fixed report structure when
the request does not need one.

Do not create, edit, move, or delete files. Do not run commands with side effects. Do not fix
reported issues or provide remediation steps, improvements, or action items unless the user
explicitly asks for them.

If another requested workflow overlaps with this skill, apply this skill as the operation boundary:
complete the requested analysis or response, but keep every action read-only.
