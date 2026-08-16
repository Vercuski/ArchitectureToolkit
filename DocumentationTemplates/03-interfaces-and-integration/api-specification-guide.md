---
title: API Specification Guide
status: draft
owner: TBD
version: 0.1
last_updated: TBD
reviewers: TBD
tags:
  - api_specificiation_guide
  - architecture
  - documentation
---
# API Specifications — {Project Name}

> Guidance: This file is a pointer/README, not the spec itself. Keep
> actual contracts in machine-readable form (OpenAPI/Swagger for REST,
> AsyncAPI for event-driven, .proto for gRPC) so they can be linted and
> used for mock/codegen — don't hand-write API docs in prose if you can
> avoid it.

## 1. Inventory

| API | Type | Spec Location | Owning Team | Status |
|---|---|---|---|---|
| TBD | REST / gRPC / async | `/specs/TBD.yaml` | TBD | Draft/Stable/Deprecated |

## 2. Versioning Strategy
TBD — e.g., URI versioning (`/v1/...`), header-based, semantic versioning
of the contract itself.

## 3. Deprecation Policy
TBD — how much notice, how long old versions are supported.

## 4. Authentication & Authorization
TBD — link to `../05-security/security-architecture.md`

## 5. Error Handling Convention
> Guidance: Standardize error shape across all APIs (e.g., RFC 7807
> Problem Details) so consumers write one error handler, not one per API.

TBD

## 6. Example OpenAPI Skeleton
```yaml
openapi: 3.0.3
info:
  title: TBD
  version: 1.0.0
paths:
  /example:
    get:
      summary: TBD
      responses:
        '200':
          description: TBD
```
