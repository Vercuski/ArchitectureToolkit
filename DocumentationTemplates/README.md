# ArchitectureToolkit

Software Architecture Toolkit — a consolidated library of architecture documentation templates, organized by phase of the architecture lifecycle: vision → requirements → design → decisions → governance → operations → migration → validation → handover.

This README is the index (a "Map of Content") for the template library. Every template lives under `DocumentationTemplates/`, in one of 12 numbered folders. Numbering reflects a *typical* order of use, not a mandatory sequence — pull whichever documents fit your engagement.

## How to use this library

1. Copy the templates you need into your project's own docs folder (or reference this repo directly if you're working in Obsidian — each template is a self-contained note with frontmatter, ready to link into a vault).
2. Fill in the `{Project Name}` placeholders and `TBD` fields.
3. Update the frontmatter (`status`, `owner`, `version`, `last_updated`, `reviewers`) as the document matures.
4. Follow the cross-references — most templates link to related documents rather than duplicating content. Keep it that way; a template that grows to duplicate another one is a sign it should just link out instead.

### Frontmatter conventions

Every template shares the same frontmatter block:

| Field | Meaning |
|---|---|
| `title` | Document title |
| `status` | `draft` / `in-review` / `approved` / etc. |
| `owner` | Accountable individual or team |
| `version` | Semantic-ish version of the document itself |
| `last_updated` | Date of last substantive edit |
| `reviewers` | Who signed off (or needs to) |
| `tags` | Used for Obsidian search/graph filtering |

## Template index

### 00 — Vision & Strategy
| Template | Purpose |
|---|---|
| [Architecture Vision](00-vision-and-strategy/architecture-vision.md) | The elevator pitch — problem statement, goals, stakeholders, scope, and success criteria. Start here for any new initiative. |
| [Business Case](00-vision-and-strategy/business-case.md) | Options considered, cost/benefit analysis, ROI, and recommendation to justify pursuing the initiative. |
| [Architecture Roadmap](00-vision-and-strategy/architecture-roadmap.md) | Phased plan bridging current state to target state, with gap analysis and milestones. |

### 01 — Requirements
| Template | Purpose |
|---|---|
| [Business Requirements (BRD)](01-requirements/business-requirements.md) | What the business needs, independent of any technical solution. |
| [Functional Requirements (FRS)](01-requirements/functional-requirements.md) | Functional requirements with a traceability matrix back to business requirements. |
| [Non-Functional Requirements](01-requirements/non-functional-requirements.md) | Quality attributes (performance, scalability, availability, security, etc.) with measurable scenarios and trade-off priorities. |
| [Use Cases / User Stories](01-requirements/use-cases-and-user-stories.md) | Use case and user-story templates with Given/When/Then acceptance criteria. |

### 02 — Core Architecture
| Template | Purpose |
|---|---|
| [Solution Architecture Document (SAD)](02-core-architecture/solution-architecture-document.md) | The umbrella document — a table of contents with connective narrative, linking out to everything below. |
| [Domain Model](02-core-architecture/domain-model.md) | Bounded contexts, ubiquitous language, entities, and aggregates — the conceptual, DDD-flavored counterpart to Data Architecture. |
| [Data Architecture](02-core-architecture/data-architecture.md) | Physical/logical data concerns — storage, flow, retention, governance, classification. |
| [Sequence Diagrams](02-core-architecture/sequence-diagrams.md) | Interaction diagrams for non-obvious, cross-service, or business-critical flows only. |
| [4+1 Architectural Views](02-core-architecture/4plus1-views.md) | Kruchten's 4+1 view model — an alternative or complement to C4. |
| [C4 — Context Diagram](02-core-architecture/c4-model/context-diagram.md) | C4 Level 1 — the system as a black box and its actors/external systems. |
| [C4 — Container Diagram](02-core-architecture/c4-model/container-diagram.md) | C4 Level 2 — high-level technical building blocks and how they communicate. |
| [C4 — Component Diagram](02-core-architecture/c4-model/component-diagram.md) | C4 Level 3 — internal components of a single container. |

### 03 — Interfaces & Integration
| Template | Purpose |
|---|---|
| [Integration Architecture](03-interfaces-and-integration/integration-architecture.md) | Integration patterns, API inventory, message/event contracts, versioning strategy. |
| [API Specification Guide](03-interfaces-and-integration/api-specification-guide.md) | Pointer/README into machine-readable specs (OpenAPI/AsyncAPI/proto) plus error-handling conventions. |
| [Interface Control Document (ICD)](03-interfaces-and-integration/interface-control-document.md) | Formal, bilateral interface agreement — for cross-org or vendor boundaries. |

### 04 — Infrastructure & Network
| Template | Purpose |
|---|---|
| [Network Architecture](04-infrastructure-and-network/network-architecture.md) | Network zones, trust boundaries, connectivity, firewall/security group rules. |
| [Deployment Topology](04-infrastructure-and-network/deployment-topology.md) | Environments, topology diagram, scaling strategy, environment parity notes. |
| [Infrastructure-as-Code Notes](04-infrastructure-and-network/infrastructure-as-code-notes.md) | The map into the IaC repo — module structure, state management, promotion process. |

### 05 — Security
| Template | Purpose |
|---|---|
| [Security Architecture](05-security/security-architecture.md) | AuthN/AuthZ, trust boundaries, data classification, encryption, secrets, audit logging. |
| [Threat Model (STRIDE)](05-security/threat-model.md) | STRIDE-based threat identification walked per trust boundary. |

### 06 — Decisions & Standards
| Template | Purpose |
|---|---|
| [Architecture Principles](06-decisions-and-standards/architecture-principles.md) | Register of guiding principles, each with rationale and implications. |
| [Coding Standards / Guidelines](06-decisions-and-standards/coding-standards.md) | Naming, structure, error handling, testing, and linting standards. |
| [Technology Evaluation / Trade Study](06-decisions-and-standards/technology-evaluation-trade-study.md) | Weighted scoring comparison between competing technologies/products — do this before writing the ADR. |
| [Reference Architecture & Technology Radar](06-decisions-and-standards/reference-architecture-and-technology-radar.md) | Approved stack, reference patterns, deprecated tech, and an Adopt/Trial/Assess/Hold radar. |
| [ADR Template](06-decisions-and-standards/adr/adr-template.md) | Michael Nygard–format Architecture Decision Record — one per file, numbered sequentially. |
| [ADR Log](06-decisions-and-standards/adr/adr-log.md) | Index of all ADRs. |
| [ADR-0001: Record Architecture Decisions](06-decisions-and-standards/adr/0001-record-architecture-decisions.md) | The seed ADR — adopting ADRs as a practice. |

### 07 — Governance
| Template | Purpose |
|---|---|
| [Governance Framework](07-governance/governance-framework.md) | Roles, responsibilities, review process, and escalation path for architecture governance. |
| [RFC Template](07-governance/rfc-template.md) | Proposal template for significant changes, reviewed before becoming an ADR. |
| [ARB Minutes Template](07-governance/arb-minutes-template.md) | Architecture Review Board meeting minutes — decisions and action items. |
| [Compliance Matrix](07-governance/compliance-matrix.md) | Applicable regulations/standards mapped to controls, evidence, and status. |
| [Architecture Exception Request](07-governance/architecture-exception-request.md) | Formal, time-boxed request to deviate from an approved standard. |

### 08 — Risk & Operations
| Template | Purpose |
|---|---|
| [Risk Register](08-risk-and-operations/risk-register.md) | Scored register of technical/business/security/ops risks. |
| [Capacity Planning](08-risk-and-operations/capacity-planning.md) | Current load profile, growth projections, scaling strategy, cost implications. |
| [SLA / SLO / SLI Definitions](08-risk-and-operations/sla-slo-sli-definitions.md) | SLIs, SLOs, SLAs, and error-budget policy — kept distinct so they aren't confused. |
| [Observability Strategy](08-risk-and-operations/observability-strategy.md) | Unified metrics/logging/tracing strategy, dashboards, alerting, and retention. |
| [Runbook Template](08-risk-and-operations/runbook-template.md) | Per-scenario operational runbook — symptoms, diagnosis, resolution, escalation. |
| [Disaster Recovery Plan](08-risk-and-operations/disaster-recovery-plan.md) | RTO/RPO targets, backup strategy, and recovery procedures per disaster scenario. |
| [Incident Postmortem](08-risk-and-operations/incident-postmortem.md) | Blameless postmortem — timeline, 5-whys root cause, and action items. |

### 09 — Transition & Migration
| Template | Purpose |
|---|---|
| [Gap Analysis](09-transition-and-migration/gap-analysis.md) | Current-state vs. target-state gap table with recommendations. |
| [Transition Architecture](09-transition-and-migration/transition-architecture.md) | Interim architecture states between current and target, with exit criteria per state. |
| [Migration Plan](09-transition-and-migration/migration-plan.md) | Migration strategy, waves, cutover plan, and rollback strategy. |
| [Data Migration Strategy](09-transition-and-migration/data-migration-strategy.md) | Source/target mapping, ETL/ELT approach, validation & reconciliation, PII considerations. |

### 10 — Testing & Validation
| Template | Purpose |
|---|---|
| [Test Strategy](10-testing-and-validation/test-strategy.md) | Testing levels and, specifically, how each NFR gets validated (load, chaos, security, accessibility). |
| [Architecture Compliance Review](10-testing-and-validation/architecture-compliance-review.md) | Checklist-based review against the reference architecture, ADRs, and NFRs. |

### 11 — Handover
| Template | Purpose |
|---|---|
| [Onboarding / Knowledge Transfer](11-handover/onboarding-knowledge-transfer.md) | Quick start, local dev setup, key repos, who-to-ask, glossary, FAQ. |
| [As-Built Documentation](11-handover/as-built-documentation.md) | What was actually deployed — deviations from design, environments, known issues/tech debt. |

## Suggested reading order

The folder numbering roughly follows a full engagement end to end:

```
00 Vision & Strategy      → why we're doing this
01 Requirements           → what it needs to do
02 Core Architecture      → how it's shaped
03 Interfaces             → how it talks to other systems
04 Infrastructure         → where it runs
05 Security               → how it's protected
06 Decisions & Standards  → why we chose what we chose
07 Governance             → how choices get reviewed and approved
08 Risk & Operations      → how it stays healthy in production
09 Transition & Migration → how we get from old to new
10 Testing & Validation   → how we know it actually works
11 Handover                → how the next person picks it up
```

Not every project needs every document — a greenfield internal tool may skip `09-transition-and-migration` entirely, while a legacy replacement will lean on it heavily. Treat the numbering as a menu, not a checklist.

## Cross-cutting notes

- **ADRs vs. Trade Studies vs. RFCs**: an RFC proposes a change and invites discussion; a Trade Study scores the options once a decision is contested or has more than two viable candidates; an ADR records the outcome, win or lose. Trade Study → ADR is the normal path for anything non-trivial.
- **Observability vs. Security logging**: operational metrics/logs/traces live in `08-risk-and-operations/observability-strategy.md`; audit logging for compliance purposes lives in `05-security/security-architecture.md` — don't duplicate between them, link instead.
- **Domain Model vs. Data Architecture**: `domain-model.md` is the conceptual/DDD side (ubiquitous language, aggregates); `data-architecture.md` is the physical/logical side (storage, retention, PII). Keep the split.

## Contributing

When adding a new template:
1. Place it in the folder that matches its lifecycle phase (create a new numbered folder only if none of the 12 fit).
2. Use the shared frontmatter block.
3. Link out to related templates rather than duplicating their content.
4. Add a row to this README's index table.

## License

MIT — see [LICENSE](../LICENSE).
