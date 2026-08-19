---
title: SLA / SLO / SLI Definitions & Error Budget
status: draft
owner: TBD
version: 0.1
last_updated: TBD
reviewers: TBD
tags:
  - sla
  - slo
  - sli
---

# SLOs, SLAs & Error Budgets — {Project Name}

> Guidance: SLI = the actual measured metric. SLO = internal target for
> that metric (stricter than the SLA, giving you buffer). SLA = external,
> often contractual, promise — usually with penalties. Define all three so
> they're not confused.

## 1. Service Level Indicators (SLIs)
| SLI | Definition | Measurement Method |
|---|---|---|
| Availability | % successful requests | TBD (e.g., synthetic monitoring) |
| Latency | p50/p95/p99 response time | TBD |
| Error rate | % of 5xx responses | TBD |

## 2. Service Level Objectives (SLOs)
| Service | SLI | SLO Target | Measurement Window |
|---|---|---|---|
| | Availability | 99.9% | Rolling 30 days |
| | Latency (p95) | | |

## 3. Service Level Agreements (SLAs)
> Guidance: Only fill this in if there's an actual contractual/customer-
> facing commitment — internal projects often only need SLOs.

| Commitment | Target | Penalty for Breach |
|---|---|---|
| TBD | TBD | TBD |

## 4. Error Budget
- Budget = 100% - SLO target (e.g., 99.9% SLO = 0.1% budget ≈ 43 min/month)
- Policy when budget exhausted: TBD (e.g., freeze feature releases, focus
  on reliability)

## 5. Ownership
| SLO | Owning Team | Escalation Contact |
|---|---|---|
| TBD | TBD | TBD |
