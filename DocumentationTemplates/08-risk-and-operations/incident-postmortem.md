---
title: "Incident Postmortem: {Incident Name}"
status: draft
owner: TBD
version: 0.1
last_updated: TBD
reviewers: TBD
tags:
  - incident_postmortem
  - architecture
  - documentation
---
# Incident Postmortem — {Incident Name}

> Guidance: This is blameless. The goal is to understand what happened
> and why the system (including people and process) allowed it to
> happen — not to assign fault. Write it assuming the people involved
> made reasonable decisions given what they knew at the time. This is
> the retrospective counterpart to a runbook: a runbook tells you what
> to do during an incident; this document tells you what to learn from
> one afterward. Complete it within a few days of resolution, while
> details are still fresh.

| Field | Value |
|---|---|
| Incident ID | |
| Date / Time (start) | |
| Date / Time (resolved) | |
| Duration | |
| Severity | Sev1 / Sev2 / Sev3 / Sev4 |
| Author | |
| Status | Draft / Under Review / Final |

## 1. Summary
One paragraph: what happened, what triggered it, how it was resolved.
Written so someone outside the incident can understand it in 30 seconds.

## 2. Impact
| Area | Impact |
|---|---|
| Users affected | |
| Services affected | |
| Data affected | |
| Revenue / SLA impact | |
| External communication issued? | Y/N — link if so |

## 3. Timeline
> Guidance: Use UTC (or state the timezone) and be precise — this
> section is what most future readers will actually study. Include
> detection, escalation, mitigation, and resolution, not just the root
> cause moment.

| Time | Event |
|---|---|
| | Incident begins |
| | Detected (how? alert, customer report, manual discovery) |
| | Escalated / on-call engaged |
| | Root cause identified |
| | Mitigation applied |
| | Incident resolved |
| | Postmortem completed |

## 4. Root Cause Analysis
> Guidance: Push past the first plausible answer. "5 Whys" is one way
> to do this — keep asking why until you reach something actionable
> (a process gap, a missing safeguard, a design assumption that broke),
> not just a proximate technical cause.

**Proximate cause:**

**5 Whys:**
1. Why did the incident happen?
2. Why did *that* happen?
3. Why did *that* happen?
4. Why did *that* happen?
5. Why did *that* happen?

**Root cause:**

## 5. Contributing Factors
> Guidance: Rarely is there one cause. List the conditions that made
> this possible or made it worse — missing monitoring, insufficient
> testing, a recent change, capacity limits, unclear ownership, etc.

-

## 6. Detection & Response Assessment
| Question | Answer |
|---|---|
| Did monitoring/alerting catch this? If not, why not? | |
| Was the correct runbook available and followed? (link if applicable) | |
| Was escalation timely? | |
| What slowed down diagnosis or mitigation? | |

## 7. What Went Well / What Went Poorly
**Went well:**
-

**Went poorly:**
-

**Where we got lucky:**
> Guidance: Distinguish genuine mitigations from things that just
> happened not to make it worse — luck isn't a strategy, and naming it
> honestly prevents false confidence next time.

-

## 8. Action Items
| Action | Type (Prevent/Detect/Mitigate) | Owner | Due Date | Status |
|---|---|---|---|---|
| | | | | Open |

## 9. Related
- Runbook(s) invoked: `../08-risk-and-operations/runbook-template.md`
- Related incidents:
- Related ADRs / risk register entries: `../08-risk-and-operations/risk-register.md`