# GitHub Body Publication and Metadata Synchronization

Use this reference when creating or updating an Issue or Pull Request body, or when synchronizing
GitHub Project and PR metadata after a lifecycle transition. The remote GitHub artifact remains the
source of truth; a local file is only a short-lived publication draft.

## Body publication

For a non-trivial Issue or PR body, the agent MUST use an external temporary draft and the GitHub
CLI's file input rather than putting the complete body in a command argument or repeatedly printing
it in the conversation:

1. Read the latest remote body once and preserve it as the base for the edit.
2. Create a private temporary directory outside the repository, preferably under `/tmp`, and write
   the draft there. Do not commit, attach, or treat the draft as a durable specification.
3. Apply the body change to the draft and validate the required sections, language, placeholders,
   Primary Issue, stack context, and other applicable publication gates locally.
4. Publish with `gh issue create --body-file <draft>`, `gh issue edit --body-file <draft>`,
   `gh pr create --body-file <draft>`, or `gh pr edit --body-file <draft>`, as applicable. Do not
   inline a long body with `--body`.
5. Read the remote body back and compare it with the normalized draft. Report only the publication
   result, size/hash, and any mismatch; do not echo the complete body into the conversation.
6. Remove the temporary draft after successful verification, or preserve its path only while
   reporting a failed synchronization that must be reconciled.

The agent MUST refresh the remote body or verify its last-seen version immediately before
publishing. If another update changed the body after the draft was based on it, stop and merge the
new remote content before retrying. Never overwrite a concurrent update with a stale draft.

Line-ending normalization MAY be used for verification, but a mismatch in substantive content MUST
fail publication verification. If the mutation succeeds but read-back verification fails, preserve
the objective remote fact, report the mismatch, and reconcile later; do not blindly publish again.

Use the same protocol for a new body when the CLI supports `--body-file`. A short body MAY use
`--body`, but the agent SHOULD prefer file input whenever the body contains Markdown structure,
multiple sections, or machine-readable metadata.

## Structured metadata

Do not encode a native GitHub state as the authority inside a Markdown body:

- use `gh pr ready` or `gh pr ready --undo` for PR Draft/Ready state;
- use `gh project item-edit` or the applicable Project API for Project Status and other fields;
- use the dedicated CLI/API parameters for labels, assignees, milestones, and reviewers.

If a body contains a human-readable mirror of a structured value, update that mirror through the
same temporary draft protocol after the authoritative mutation. A body mirror MUST NOT be used to
infer the current Project or PR state when the native value is available.

Lifecycle synchronization MAY update several native fields in one workflow operation. The user
must not be asked to edit the PR body merely to record a state transition. In particular, the agent
owns derived PR fields such as the current reviewed head and `Human Review: Complete` after the
applicable human approval evidence is verified.

## Token and output discipline

Prefer `--json` with a narrow `--jq`/template selection and local comparisons over printing full
Issue, PR, or Project payloads. The agent should return a compact success/failure summary and only
the exact mismatch or blocker, not the entire body that was already staged locally.
