---
title: "Architecture Exception Request: {Short Title}"
status: draft
owner: TBD
version: 0.1
last_updated: TBD
reviewers: TBD
tags:
  - exception_request
  - governance
---
# Architecture Exception Request — \<Short Title\>

> Guidance: Use this when a team needs to deviate from an approved
> standard — a reference architecture, technology radar entry, coding
> standard, or architecture principle — and can't or shouldn't wait for
> that standard itself to change. It's the formal counterpart to
> `rfc-template.md`: an RFC proposes a new decision, this requests
> permission to temporarily (or permanently) not follow an existing
> one. Every exception should have an expiry or a review trigger —
> an exception with no end date is just an unreviewed standards change.

| Field | Value |
|---|---|
| Requesting Team | |
| Requested By | |
| Date | YYYY-MM-DD |
| Status | Proposed / Approved / Rejected / Expired |
| Standard Being Excepted | link to the specific principle, radar entry, or standard |
| Expiry / Review Date | |

## 1. Standard Being Excepted
> Guidance: Quote or link the specific rule, not just the document it
> lives in — e.g. a specific row in
> `../06-decisions-and-standards/reference-architecture-and-technology-radar.md`
> §3 (Deprecated / Disallowed Technologies) or a specific principle in
> `../06-decisions-and-standards/architecture-principles.md`.

## 2. Reason for Exception
Why the standard can't be followed here — technical constraint, vendor
limitation, timeline pressure, legacy integration, etc.

## 3. Scope
- **What this exception covers:**
- **What it does NOT cover:** (prevents scope creep into a de facto
  standards change)
- **Systems/services affected:**

## 4. Risk Assessment
| Risk | Impact | Likelihood | Mitigation |
|---|---|---|---|
| | | | |

## 5. Alternatives Considered
| Alternative | Why Not Chosen |
|---|---|
| Following the standard as-is | |
| | |

## 6. Compensating Controls
> Guidance: What's being done to limit the blast radius while the
> exception is in effect — extra monitoring, restricted scope, manual
> review gates, etc.

-

## 7. Expiry & Review
- **Expiry date or trigger condition:**
- **What happens at expiry:** revert to standard / becomes permanent
  via a standards change / re-request
- **Review cadence until expiry:**

## 8. Approval
| Role | Name | Decision | Date |
|---|---|---|---|
| Architecture Review Board | | | |
| Standard Owner | | | |

## 9. Related
- Standard excepted:
  `../06-decisions-and-standards/reference-architecture-and-technology-radar.md`
- Resulting ADR, if the exception becomes permanent:
  `../06-decisions-and-standards/adr/`
- Related RFC, if this exception is a step toward a broader standards
  change: `rfc-template.md`
