---
tags:
  - software_architecture
  - solution_architecture
  - documentation
  - template
---
# Software / Solution Architecture Documentation Templates

A ready-to-use folder structure and starter templates for documenting a
software or solution architecture project. This set merges two prior
template libraries into one consolidated, de-duplicated set. Copy this
folder into your project or vault, delete what you don't need, and fill in
the rest.

## How to use this repo

1. **Scale to your project.** A small internal tool might only need
   `02-core-architecture/architecture-document.md`, a couple of
   `06-decisions-and-standards/adr/` entries, and a deployment diagram. A
   large enterprise migration will touch nearly every folder.
2. **Delete, don't leave blank.** Empty templates rot. If a doc doesn't
   apply, remove it rather than committing a stub nobody fills in.
3. **ADRs are the highest-leverage artifact per unit of effort.** They
   capture *why* a decision was made — something diagrams and specs can't.
   When in doubt, write an ADR.
4. **Diagrams as code.** Where possible, keep diagrams in text form
   (Mermaid, PlantUML, Structurizr DSL) alongside these docs so they live in
   version control and diff cleanly. Diagram placeholders are marked with
   `<!-- DIAGRAM -->` comments or fenced code blocks — replace with an
   embedded Mermaid diagram or a link to your diagramming tool.
5. **Link, don't duplicate.** The `architecture-document.md` is the
   umbrella/table-of-contents document. Most of its sections should link out
   to the dedicated docs in this repo rather than repeating their content.

## Conventions used in these templates

- `> Guidance:` blockquotes explain what goes in a section — delete them
  once the section is filled in.
- `TBD` marks a field that must be completed before the doc is considered
  final; `{Project Name}` and similar `{...}` placeholders should be
  replaced with real values.
- Every doc carries a small frontmatter/status block (`Status`, `Owner`,
  `Version`, `Last Updated`, `Reviewers`) so you can track document
  maturity at a glance.
- Each file works as a standalone note — if you're dropping this into
  Obsidian, add tags via frontmatter (`tags: [architecture, adr]`) to use
  Obsidian's tag search across the vault, and consider an
  `Architecture MOC.md` (Map of Content) note linking out to the key docs
  per project if you manage multiple projects in one vault.

## Folder structure

```
architecture-documentation-templates/
├── 00-vision-and-strategy/    Vision, business case, roadmap
├── 01-requirements/           BRD, FRS, NFRs, use cases
├── 02-core-architecture/      Architecture document, C4 diagrams, domain model, data architecture
├── 03-interfaces-and-integration/  API specs, ICDs, integration patterns
├── 04-infrastructure-and-network/  Deployment topology, network zones, IaC
├── 05-security/                Security architecture, threat model
├── 06-decisions-and-standards/  ADRs, principles, reference architecture, coding standards
├── 07-governance/              Governance framework, RFCs, ARB minutes, compliance matrix
├── 08-risk-and-operations/     Risk register, SLOs, runbooks, DR/BCP, capacity
├── 09-transition-and-migration/  Gap analysis, migration plan, transition architecture
├── 10-testing-and-validation/  Test strategy, architecture compliance reviews
└── 11-handover/                As-built docs, onboarding / knowledge transfer
```

## Mapping to common frameworks

- **TOGAF ADM**: `00-vision-and-strategy` ≈ Phase A · `01-requirements` +
  `02-core-architecture` ≈ Phases B/C/D · `09-transition-and-migration` ≈
  Phases E/F · `07-governance` ≈ Phase G.
- **arc42**: most of `02-core-architecture` and `06-decisions-and-standards`
  can be collapsed into a single arc42-structured document if you prefer one
  file over many.
- **C4 Model**: see `02-core-architecture/c4-model/` for Context, Container,
  and Component level templates.
- **4+1 View Model**: an alternative (or complement) to C4 lives at
  `02-core-architecture/4plus1-views.md` if your stakeholders already know
  Kruchten's model.

## Suggested minimal set (small/medium project)

- `02-core-architecture/architecture-document.md`
- `02-core-architecture/c4-model/context-diagram.md` + `container-diagram.md`
- `06-decisions-and-standards/adr/adr-template.md` (copy per decision)
- `08-risk-and-operations/risk-register.md`
- `11-handover/as-built-documentation.md`

## What changed in this merge

This library consolidates two source template sets that had grown some
overlap (two versions each of the architecture document, threat model,
disaster recovery plan, ADR template, SLO/SLA doc, runbook, and test
strategy). Each of those has been merged into a single canonical template
combining the stronger structure and content of both sources. Templates
that existed in only one source set — domain model, 4+1 views, sequence
diagrams, API spec guide, ICD template, IaC notes, technology radar, RFC
template, ARB minutes, compliance matrix, transition/migration docs, and the
handover docs — were carried over unchanged.
