---
name: grilling
description: Grill the user relentlessly about a plan, decision, or idea. Use when the user wants to stress-test their thinking, or uses any 'grill' trigger phrases.
---

Interview the user relentlessly until you reach a shared understanding. Map this as a **design tree**: every decision branches into the decisions that hang off it.

The **frontier** is every decision whose prerequisites are already settled: the questions you can ask _now_ without guessing at answers you haven't heard yet. Ask **one question at a time**. From the frontier, pick the question whose answer unblocks or reshapes the most of the tree, ask only that one, and wait for the answer before asking the next.

Ask each question with the `AskUserQuestion` tool so the user can pick an answer instead of typing one:

- `question`: the full question, ending with a question mark. Put any context the user needs to decide (what hangs on it, trade-offs) in the option descriptions, or in a short message before the tool call if it needs more room.
- `header`: a short chip label for the decision (max 12 chars).
- `options`: 2-3 concrete, mutually exclusive answers. Put your recommended answer first and append ` (Recommended)` to its label. Each `description` states what choosing it means and what it rules out.
- Always add a final option labelled `Let's discuss`, described as "Talk this question through before deciding". The tool also adds a free-text "Other" automatically; don't add one yourself.
- `multiSelect: false`, unless the decision genuinely allows several answers at once.

When the user picks `Let's discuss`, or answers via "Other" with a question or doubt rather than an answer, switch to plain conversation about that question: explain the trade-offs, answer their questions, bring in facts you look up. Don't move on while the question is open. Once the discussion has converged, re-ask the same question with `AskUserQuestion`, updating the options if the discussion surfaced new ones, so the decision is recorded explicitly.

Each answer reshapes the tree: a settled decision pushes the frontier outward and unblocks questions that depended on it. Recompute the frontier after every answer and pick the next question. Briefly acknowledge what was decided (one line) before asking it.

Finding _facts_ is your job, never the user's. When a frontier question needs a fact from the environment (filesystem, tools, etc.), dispatch a sub-agent to find it; don't ask the user for anything you could look up yourself. Don't block on it: a running exploration is an unsettled prerequisite, so only the questions downstream of it wait for the sub-agent to report; keep asking other frontier questions meanwhile. The _decisions_ are the user's: put each to them and wait.

The session is done when the frontier is empty: every branch of the design tree visited, nothing left silently assumed. Then summarise the decisions made and do not act on them until the user confirms you have reached a shared understanding.
