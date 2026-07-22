---
name: user-manual
description: |
  Produce a verified, plain-language user manual for a feature or module in the current codebase,
  aimed at a non-engineer audience (product, support, ops) unless the user names a different one.
  Runs explore, verify, and draft, then repeats a mandatory self-review loop until the document
  converges, so the user does not need to ask for another check.
  Trigger phrases: "user manual", "usage guide", "operator guide", "write documentation for",
  "使用說明書", "操作手冊", "說明文件", "非工程人員看得懂的文件", "盤點...功能並寫文件".
license: MIT
metadata:
  author: aa89227
  version: "1.0"
---

# User Manual

Produce documentation about a feature that is verified against source, readable by a non-engineer,
and self-reviewed to convergence before it reaches the user.

## Scope

State back to the user, in one line, before exploring:

1. The target feature or module — the boundary of what gets documented.
2. What is explicitly excluded — adjacent systems that touch the target but are out of scope.
3. The audience, if other than "non-engineer".
4. The output destination. Default to a scratch/temp location and open the file when done, unless
   the user names a path or an existing document to update.

Ask only when the boundary is genuinely ambiguous from the request.

If the active mode forbids writing files (e.g. discuss, readonly), do everything through Draft, but
present the draft in the response instead of running Deliver, and say why no file was created.

## Output Format

The destination from Scope item 4 decides this — pick one before drafting, then read the matching
reference file and follow it. Do not mix conventions from both in one document.

- **Markdown** when the destination is inside the repository, the document will be read in an
  editor or a PR diff, or the user names a `.md` path. Read
  `<skill-directory>/references/markdown.md`.
- **HTML** (a single self-contained file) when the document is a standalone deliverable meant to be
  opened in a browser — the default for a scratch/temp destination — or the user asks for something
  polished or presentable to a non-engineer audience. Read `<skill-directory>/references/html.md`.

## Workflow

1. **Explore.** Inventory every file touching the target across all layers it spans (domain,
   application/service, API, frontend routes, frontend components, DTOs). Use `Agent` with
   `Explore` for a large codebase. Stay inside the scope defined above; do not wander into excluded
   adjacent modules.
2. **Verify.** Read the actual source behind every claim before writing it down. Cross-check
   behavioral claims against integration/E2E tests where they exist. Do not relay UI copy, tooltips,
   or code comments as fact without confirming them against the code that is supposed to act on
   them. See "Verification Priorities" below for what to hunt for.
3. **Draft.** Write for the stated audience in the format decided under "Output Format" above. See
   "Writing Rules" below. Build the citation list during step 2, not after — every claim should
   already have a `file:line` source before it is written into the draft.
4. **Self-review.** Run the Self-Review Loop below against the full draft. This step is mandatory
   and automatic — do not stop after step 3 and wait for the user to ask for a check.
5. **Deliver.** Save to the destination decided in Scope and open it. Reply with what the document
   covers and what the self-review loop caught and fixed — not a re-paste of the document.

## Verification Priorities

Hunt for these specifically; they are the findings a single pass misses:

- **UI copy versus actual behavior.** A tooltip, hint string, or comment is a claim, not a source.
  Confirm the component or handler it describes actually behaves that way.
- **Unreachable branches.** Frontend logic keyed on a backend error string, a domain method with no
  caller, a `TODO`/`FIXME` marking something as not wired up. Search the other side of the boundary
  for the literal trigger before describing a branch as reachable.
- **Multiple paths to the same state.** When a transition can happen through more than one call
  site, trace each one separately — do not assume they produce the same side effects (events,
  notifications, tags) until confirmed.
- **Fields set but never read, or read but never set.** Check both ends independently.
- **Silent-skip edge cases.** Missing references, empty collections, idempotent early-returns rarely
  error, so the happy path won't surface them. Search for early-return guards and filters around
  anything about to be described as "always happens."

## Self-Review Loop

Re-read the entire current draft on every pass, not only the paragraphs just edited — findings from
one pass routinely live in sections written before the writer knew what to look for. Repeat all
three passes until one full round makes no changes, or after 3 rounds. If the cap is hit with
unresolved issues, say so explicitly instead of shipping a flawed draft silently.

When a pass surfaces something a reword cannot fix — a citation that does not actually hold up, a
UI-copy claim that was never checked against the code it describes — go back to Verify and confirm
it against source before touching the prose. Do not paper over a substantive gap with a
plausible-sounding rewrite.

1. **Plain-language pass.** Flag and rewrite: raw code identifiers, HTTP status codes, exception
   type names, or file paths in body text (move them to the citation appendix); undefined
   abbreviations (define at first use); implementation-mechanics vocabulary such as "atomic",
   "transaction", "idempotent", "race condition", "component" (translate into what the reader
   experiences instead).
2. **Logical-consistency pass.** For every citation in the body: confirm the appendix entry it
   points to actually supports that specific sentence, not just the same file on a related topic.
   Confirm every citation used has a matching appendix entry and every appendix entry is used
   somewhere. Re-check any place presenting "the" flow for a mechanism verification showed has more
   than one path — confirm a later edit did not re-flatten them into one sentence. Check scope words
   ("this applies to every case of X") against what the source actually covers.
3. **Tone pass.** Rewrite any sentence that narrates the verification process instead of stating the
   outcome — "we checked the code", "upon inspection", "查了原始碼", "工程團隊", "測試程式呼叫". If a
   sentence would not read the same had the fact been known from the start without opening a file,
   it is narration; rewrite it as a plain statement of what happens.

If the user has multi-agent orchestration explicitly enabled for the session and the draft is large,
these three passes may run as independent parallel reviewers over the full draft instead of one
sequential pass. Do not start that on your own initiative — it requires the same explicit opt-in as
any other multi-agent orchestration.

## Writing Rules

- State outcomes, never the fact that they were verified. The verification trail lives in the
  citation footnote, not the sentence.
- No raw code identifiers, HTTP status codes, exception types, or file paths in body text.
- No engineering-process vocabulary in body text — translate into reader-facing language.
- Define every abbreviation at first use or in an upfront glossary.
- When a mechanism has more than one path to the same outcome, present them as separate named
  paths. Do not average them into one narrative for tidiness — that is the simplification that
  hides real behavior.
- Put a known-gaps section in every document: settings with no visible effect, features that look
  present but do not work, risks already flagged in source but unfixed. This section is not
  optional, even when it makes the feature look unfinished.
- Put every technical identifier used to support a claim in a citation appendix, mapped to
  `file:line` or the exact function. Nothing in that appendix should appear unexplained in body
  text.
