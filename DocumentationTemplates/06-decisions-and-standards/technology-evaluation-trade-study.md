---
title: Technology Evaluation / Trade Study
status: draft
owner: TBD
version: 0.1
last_updated: TBD
reviewers: TBD
tags:
  - technology_evaluation
  - trade_study
---

# Technology Evaluation / Trade Study — {Decision Topic}

> Guidance: Use this when choosing between multiple viable technologies,
> products, or approaches (database engine, hosting model, message
> broker, cloud provider, etc.). It's the scored, comparative companion
> to an ADR — do the trade study first, then record the outcome as an
> ADR (see `adr/adr-template.md`) that links back here. Don't skip
> straight to an ADR for anything with more than two real options or
> where the choice is contested; the weighted scoring keeps the
> comparison honest and gives future readers your reasoning, not just
> your conclusion.

## 1. Decision Context
- **Decision to be made:**
- **Why now / what's forcing this decision:**
- **Decision owner:**
- **Deadline / driver:**
- **Related requirements:** link to relevant NFRs
  (`../01-requirements/non-functional-requirements.md`) or ADRs

## 2. Evaluation Criteria & Weights
> Guidance: Weights should sum to 100. Pull criteria from your NFRs
> where possible instead of inventing new ones — this keeps the trade
> study traceable back to `non-functional-requirements.md`.

| Criterion | Weight (%) | Description / Why It Matters |
|---|---|---|
| | | |
| | | |
| | | |

**Total: 100%**

## 3. Options Considered
| Option | Summary | Included in Scoring? | Reason if Excluded |
|---|---|---|---|
| A | | Y | |
| B | | Y | |
| C | | N | e.g. doesn't meet a hard constraint |

## 4. Scoring Matrix
> Guidance: Score each option per criterion on a consistent scale (e.g.
> 1–5, 1 = poor fit, 5 = excellent fit). Weighted score = raw score ×
> criterion weight. Don't average away a hard disqualifier — if an
> option fails a must-have constraint, exclude it in §3 rather than
> letting a high score elsewhere mask it.

| Criterion | Weight | Option A (raw / weighted) | Option B (raw / weighted) | Option C (raw / weighted) |
|---|---|---|---|---|
| | | / | / | / |
| | | / | / | / |
| | | / | / | / |
| **Total** | 100% | | | |

## 5. Detailed Option Assessments
Copy this block per option.

### 5.1 Option: \<Name\>
- **Description:**
- **Strengths:**
- **Weaknesses:**
- **Cost implications:** (licensing, hosting, operational overhead)
- **Team familiarity / learning curve:**
- **Ecosystem / community / long-term support:**
- **Migration/exit cost if we need to change later:**

## 6. Recommendation
> Guidance: State the recommended option and the deciding factors in
> full sentences — the scoring matrix supports the recommendation, it
> doesn't replace the explanation.

- **Recommended option:**
- **Deciding factors:**
- **Confidence level:** High / Medium / Low
- **Conditions that would change this recommendation:**

## 7. Consequences / Risks of Chosen Option
| Risk | Impact | Mitigation |
|---|---|---|
| | | |

**Trade-offs knowingly accepted:**
-

## 8. Related
- Resulting ADR: link once recorded (`../06-decisions-and-standards/adr/`)
- Reference architecture / tech radar entry, if applicable:
  `reference-architecture-and-technology-radar.md`
- Requirements driving this decision:
  `../01-requirements/non-functional-requirements.md`
