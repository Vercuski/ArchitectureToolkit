---
title: Security Architecture
status: draft
owner: TBD
version: 0.1
last_updated: TBD
reviewers: TBD
tags:
  - security_architecture
  - architecture
  - documentation
---
# Security Architecture — {Project Name}

## 1. Security Principles
Least privilege, defense in depth, zero trust, etc. — state which apply.

## 2. Authentication
- Mechanism: TBD (OAuth2/OIDC, SAML, API keys, mTLS)
- Identity provider: TBD
- MFA requirements: TBD

<!-- DIAGRAM: auth flow sequence diagram -->

## 3. Authorization
- Model: TBD (RBAC, ABAC, ReBAC)
- Where enforced: TBD (gateway, service, database row-level security)

## 4. Trust Boundaries
<!-- DIAGRAM: trust boundary / zone diagram -->

## 5. Data Classification
| Class | Examples | Handling Requirements |
|---|---|---|
| Public | TBD | TBD |
| Internal | TBD | TBD |
| Confidential | TBD | TBD |
| Restricted (PII/PHI/PCI) | TBD | Encryption + audit logging required |

## 6. Data Protection / Encryption
- At rest: TBD
- In transit: TBD
- Key management: TBD (KMS/HSM, rotation policy)

## 7. Secrets Management
TBD — vault solution, rotation, access control

## 8. Audit & Logging
- What's logged: TBD
- Retention: TBD
- Who can access: TBD

## 9. Threat Model Summary
Full detail in `threat-model.md`.

## 10. Compliance Requirements
TBD — GDPR, HIPAA, PCI-DSS, SOC 2, etc., and where they drive specific
controls above. See `../07-governance/compliance-matrix.md`.
