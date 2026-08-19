---
title: Infrastructure-as-Code Notes
status: draft
owner: TBD
version: 0.1
last_updated: TBD
reviewers: TBD
tags:
  - iac
---
# Infrastructure as Code — {Project Name}

> Guidance: The IaC repo itself is the living documentation. This file is
> the map into it — don't duplicate Terraform/Bicep content here.

## 1. Repository
- Location: TBD
- Tooling: Terraform / Bicep / Pulumi / CloudFormation / TBD

## 2. Module Structure
| Module | Purpose | Owner |
|---|---|---|
| TBD | TBD | TBD |

## 3. State Management
- Backend: TBD (e.g., remote state in S3 + DynamoDB lock, Terraform Cloud)
- Access control: TBD

## 4. Promotion Process
TBD — how does a change move dev → staging → prod?

## 5. Secrets Management
TBD — link to `../05-security/security-architecture.md`
