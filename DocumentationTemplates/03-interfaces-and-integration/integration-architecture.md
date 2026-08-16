---
title: Integration Architecture
status: draft
owner: TBD
version: 0.1
last_updated: TBD
reviewers: TBD
tags:
  - integration_architecture
  - architecture
  - documentation
---
# Integration Architecture — {Project Name}

## 1. Integration Overview
<!-- DIAGRAM: integration landscape diagram -->

## 2. Integration Patterns Used
Sync/async, request-response, pub/sub, event-driven, batch, etc. — and why.

## 3. API Inventory
| API | Type (REST/GraphQL/gRPC) | Consumer(s) | Spec Location |
|---|---|---|---|
| | | | e.g. `openapi.yaml` — see `api-specification-guide.md` |

## 4. Message/Event Contracts
| Event/Message | Producer | Consumer(s) | Schema Location |
|---|---|---|---|
| | | | |

## 5. Error Handling & Retry Strategy

## 6. Versioning Strategy

## 7. Third-Party Integrations
| System | Purpose | Auth Method | SLA |
|---|---|---|---|
| | | | |

## 8. Formal Interface Agreements
For interfaces requiring bilateral sign-off across org/team boundaries, use
`interface-control-document.md`.
