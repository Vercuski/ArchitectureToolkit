---
title: "ICD: {System A} <-> {System B}"
status: draft
owner: TBD
tags:
  - interface_control_doucment
  - architecture
  - documentation
---
# Interface Control Document — {System A} ↔ {System B}

> Guidance: ICDs are for interfaces where formal, bilateral sign-off
> matters — often across org/team boundaries, or with external
> vendors/partners. Overkill for two services owned by the same team.

## Parties
| System | Owning Team | Contact |
|---|---|---|
| TBD | TBD | TBD |

## Interface Description
- **Protocol:** TBD (REST, SOAP, file transfer, message queue, etc.)
- **Direction:** TBD (A→B, B→A, bidirectional)
- **Frequency/Trigger:** TBD (real-time, batch nightly, on-demand)

## Data Exchanged
| Field | Type | Required | Description | Owner |
|---|---|---|---|---|
| TBD | TBD | Y/N | TBD | TBD |

## Error Handling & Retry
TBD

## SLA
- Availability: TBD
- Latency: TBD
- Throughput: TBD

## Change Management
> Guidance: How do changes to this interface get proposed, reviewed, and
> rolled out without breaking the other party?

TBD

## Sign-off
| Role | Name | Date |
|---|---|---|
| System A Approver | TBD | TBD |
| System B Approver | TBD | TBD |
