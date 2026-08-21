# ForgeCare Early Tester Program

ForgeCare is looking for a small first group of technically experienced Windows users to test the real workflow — not just whether the application launches.

## What we want to learn

The first validation target is **5–10 external testers**. This is not a download-count campaign. We want evidence about whether ForgeCare is useful, understandable and trustworthy on machines outside the development environment.

The strongest early signal is simple:

> After trying ForgeCare once, would you voluntarily use it on a second Windows machine?

## Who is a useful tester?

Especially useful perspectives include:

- IT support and onsite technicians
- helpdesk / desktop support staff
- homelab and power users
- PC repair technicians
- Windows administrators
- developers who maintain their own Windows workstations

You do not need to be an expert, but you should be comfortable describing what you expected and what happened.

## Before testing

ForgeCare v1.0.0 is an early public release. Treat it as technician-oriented software, review proposed actions before applying them, and do not test disruptive maintenance actions on a production-critical machine unless you understand their effect.

Do not share credentials, customer-sensitive information or unreviewed diagnostic exports publicly. Review Debug Bundles and screenshots before attaching them to GitHub issues.

## Suggested 20–30 minute test

### 1. First impression

Without reading every document first, launch ForgeCare and answer:

- Is it obvious what the product is for?
- What would you click first?
- Is anything immediately confusing or suspicious?

### 2. Scan and Analyze

Run the available assessment/analysis workflow.

Observe:

- Did the information feel relevant?
- Was anything difficult to understand?
- Did you trust the findings?
- What information did you still need another Windows tool to obtain?

### 3. Plan

Review the proposed work before applying anything.

Observe:

- Is the difference between observation, recommendation and action clear?
- Do you understand why each proposed action exists?
- Is there enough information to make a technician decision?

### 4. Forge

Only apply actions you are comfortable testing.

Observe:

- Did ForgeCare clearly communicate what it was going to do?
- Did anything behave differently than expected?
- Were warnings and safety boundaries adequate?

### 5. Verify

After actions complete, inspect the verification/result state.

Ask yourself:

- Can I tell whether the intended change actually happened?
- Is the before/after result useful?
- Would this help me explain completed work to another technician or user?

### 6. Report

Inspect any available report/output workflow.

Ask:

- Is this something I would keep with a support case?
- What is missing from the report?
- Is anything present that should not be shared externally?

### 7. The second-machine test

Finally answer:

> Would you install/run ForgeCare on another Windows machine without being asked?

If **yes**, tell us why.

If **no**, tell us what would need to change first.

## Feedback questions

Useful feedback does not need to be long. These questions matter most:

1. What was genuinely useful?
2. Where did you become confused?
3. Where, if anywhere, did you lose trust in the application?
4. What task still required another tool?
5. Did Verify add value?
6. Would you keep/use the Report?
7. Would you use ForgeCare on a second machine?
8. What is the single most important change you would make?
9. After using it, could you imagine paying for a mature professional version? If so, what purchasing model would feel natural: one-time license, technician license, subscription, or something else?

## How to send feedback

Use the repository's **Feature or workflow feedback** issue form for general workflow feedback and the **Bug report** form for reproducible problems.

For security-sensitive findings, follow `SECURITY.md` rather than posting details publicly.

## What happens to feedback?

Feedback will be grouped into four buckets:

1. **Trust / safety** — anything that prevents confident use
2. **Reliability** — failures on real external machines
3. **Workflow clarity** — friction across Scan → Analyze → Plan → Forge → Verify → Report
4. **Capability gaps** — tasks testers repeatedly need another tool to complete

Trust and reliability outrank feature expansion for the next release. Repeated external evidence outranks speculative feature ideas.

## Success criteria for the first cohort

The first cohort is successful when we have enough evidence to answer:

- Can external users complete the core workflow?
- Which parts create or destroy trust?
- Which capability gaps recur across testers?
- Does Verify/Report provide meaningful differentiation?
- Does anyone voluntarily return for a second-machine use?
- Is there an early willingness-to-pay signal worth validating further?

Five thoughtful test sessions are more useful than hundreds of unobserved downloads.
