---
title: Key Sequence / Interaction Diagrams
status: draft
owner: TBD
tags:
  - sequence_diagram
  - architecture
  - documentation
---
# Sequence Diagrams — {Project Name}

> Guidance: Only diagram flows that are non-obvious, cross multiple
> services, or are business-critical (checkout, auth, payment). Don't
> diagram every CRUD call.

## Flow: {Name, e.g., "User Checkout"}

```mermaid
sequenceDiagram
  participant U as User
  participant W as Web App
  participant A as API
  participant D as Database
  U->>W: Submit order
  W->>A: POST /orders
  A->>D: Insert order
  D-->>A: OK
  A-->>W: 201 Created
  W-->>U: Order confirmation
```

**Notes:**
- Failure modes: TBD
- Idempotency considerations: TBD
- Timeout/retry behavior: TBD
