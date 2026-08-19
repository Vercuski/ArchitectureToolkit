---
title: Non-Functional Requirements / Quality Attributes
status: draft
owner: TBD
version: 0.1
last_updated: TBD
reviewers: TBD
tags:
  - non_functional_requirements
---

# Non-Functional Requirements — {Project Name}

> Guidance: Vague NFRs ("the system should be fast") are useless. Use the
> quick-reference tables below for day-to-day tracking, and write a full
> quality attribute scenario (Source, Stimulus, Environment, Artifact,
> Response, Response Measure) for any NFR that is architecturally
> significant or contested.

## 1. Performance
| Metric | Target | Notes |
|---|---|---|
| p95 latency | | |
| Throughput | | |

**Scenario:** Source: User | Stimulus: Submits order | Environment: Peak
load | Artifact: Order service | Response: Order accepted | Response
Measure: p99 < 300ms

## 2. Scalability
- Expected growth (users, data, traffic)
- Horizontal vs vertical scaling strategy

## 3. Availability
| Metric | Target |
|---|---|
| Uptime SLA | e.g. 99.9% |
| RTO | |
| RPO | |

**Scenario:** Source: TBD | Stimulus: Node failure | Environment:
Production | Artifact: TBD | Response: Failover | Response Measure: < 30s
downtime

## 4. Security
- AuthN/AuthZ requirements
- Data classification & encryption requirements
- Compliance (SOC2, GDPR, HIPAA, etc.) — cross-reference
  `../05-security/security-architecture.md`

## 5. Maintainability
- Code quality gates
- Documentation standards

## 6. Observability
- Logging, metrics, tracing requirements

## 7. Other Quality Attributes
Usability, portability, interoperability, cost efficiency, etc.

## 8. Prioritization / Trade-offs
> Guidance: Quality attributes trade off against each other. State which
> ones win when they conflict (e.g., "consistency over availability during
> checkout").

1. TBD
2. TBD
