---
name: summary
description: |
  Summarize completed agent work around the original need, outcomes, current state, validation,
  and remaining limitations while suppressing irrelevant implementation detail. Use for the most
  recent work request by default, the whole session when requested, or a user-defined scope,
  including summary, summarize, 總結, and 摘要 requests.
license: MIT
metadata:
  author: aa89227
  version: "1.1"
---

# Summary Mode

Produce a self-contained retrospective summary in the user's language without changing state.

## Determine Scope

Choose the scope from the user's wording:

- **Last work request**: Use for a bare summary request and phrases such as "just did", "last
  instruction", or "this change". Find the most recent user instruction that initiated actual work;
  do not treat the summary request itself as work.
- **Whole session**: Use for phrases such as "session", "everything so far", "to this point", or
  "整個對話".
- **Custom scope**: Use the branch, commit range, PR, task, topic, or conversation boundary named by
  the user.

Ask for clarification only when the boundary cannot be identified reliably.

Use conversation history and recorded tool results as the primary source for session and last-work
summaries. Use read-only Git inspection to verify current state or when the user explicitly requests
a Git-based scope. A diff alone does not prove that every included change was produced in this
session.

If context compaction or missing history prevents a complete summary, state that the summary covers
only the context still available.

## Select Relevant Detail

Organize the summary around:

1. The need or intended outcome.
2. What was completed at a behavior or decision level.
3. The resulting current state.
4. Material validation that was actually performed.
5. Remaining work, limitations, blockers, or risks.

Include a technical detail only when it:

- is part of the original requirement;
- changes externally meaningful behavior;
- explains a material decision, risk, failure, or limitation; or
- is necessary for someone to continue the work.

Usually omit file-by-file narration, internal identifiers, raw command output, incidental refactors,
discarded attempts, and tool-call chronology. Do not claim validation that was not performed.

## Output

- Start with a one-line statement of the overall intent or result.
- Use a short paragraph for a small change.
- Use flat bullets grouped by outcome for multiple distinct changes.
- Prefer conclusions and current status over implementation chronology.
