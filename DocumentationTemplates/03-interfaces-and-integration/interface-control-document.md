---
title: "ICD: {System A} <-> {System B}"
status: draft
owner: TBD
version: 1.0.0
last_updated: TBD
reviewers: TBD
tags:
  - interface_control_document
---
# Interface Control Document — {System A} ↔ {System B}

> Guidance: ICDs are for interfaces where formal, bilateral sign-off
> matters — often across org/team boundaries, or with external
> vendors/partners. Overkill for two services owned by the same team.

## 1. Parties
| System | Owning Team | Contact |
|---|---|---|
| TBD | TBD | TBD |

## 2. Interface Description
- **Protocol:** TBD (REST, SOAP, file transfer, message queue, etc.)
- **Direction:** TBD (A→B, B→A, bidirectional)
- **Frequency/Trigger:** TBD (real-time, batch nightly, on-demand)

## 3. Data Exchanged
| Field | Type | Required | Description | Owner |
|---|---|---|---|---|
| TBD | TBD | Y/N | TBD | TBD |

## 4. Error Handling & Retry
TBD

## 5. SLA
- Availability: TBD
- Latency: TBD
- Throughput: TBD

## 6. Change Management
> Guidance: How do changes to this interface get proposed, reviewed, and
> rolled out without breaking the other party?

TBD

## 7. Sign-off
| Role | Name | Date |
|---|---|---|
| System A Approver | TBD | TBD |
| System B Approver | TBD | TBD |
