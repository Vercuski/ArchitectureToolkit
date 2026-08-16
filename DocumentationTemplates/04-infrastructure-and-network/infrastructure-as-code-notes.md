---
title: Infrastructure-as-Code Notes
status: draft
owner: TBD
tags:
  - IaC
  - architecture
  - documentation
---
# Infrastructure as Code — {Project Name}

> Guidance: The IaC repo itself is the living documentation. This file is
> the map into it — don't duplicate Terraform/Bicep content here.

## Repository
- Location: TBD
- Tooling: Terraform / Bicep / Pulumi / CloudFormation / TBD

## Module Structure
| Module | Purpose | Owner |
|---|---|---|
| TBD | TBD | TBD |

## State Management
- Backend: TBD (e.g., remote state in S3 + DynamoDB lock, Terraform Cloud)
- Access control: TBD

## Promotion Process
TBD — how does a change move dev → staging → prod?

## Secrets Management
TBD — link to `../05-security/security-architecture.md`
