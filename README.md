# ArchitectureToolkit

A self-hostable, multi-user web application for authoring, versioning, and
governing architecture documentation — built around a curated library of
markdown templates.

> **Status:** under active development. Core architecture decisions are
> recorded and largely settled; application implementation is in progress.
> Not yet ready for production use.

## What this is

ArchitectureToolkit lets a team of architects populate a set of file
templates — vision documents, ADRs, domain models, security architecture,
and more — under individual projects. Every save prompts a semantic
version bump (Major/Minor/Patch) and produces a full, attributed,
append-only revision history: nothing is ever silently overwritten, and
every change is traceable to who made it and why.

The goal is a tool with strong governance mechanics and a low operational
burden — one `docker compose up`, no separate services to wire together,
no manual setup beyond that.

## Two things live in this repo

**The application** — the .NET 10 / Vue 3 web app itself, described
below.

**[`DocumentationTemplates/`](DocumentationTemplates/)** — a
self-contained library of 50 architecture documentation templates,
organized across 12 lifecycle-phase folders. It's fully usable on its
own: copy the templates into any project's docs folder, or reference
them directly from an Obsidian vault, with or without ever running the
application. See [`DocumentationTemplates/README.md`](DocumentationTemplates/README.md)
for the full index. When the application starts up for the first time,
it seeds this same library into its own database automatically — the
two stay independent after that point (the application doesn't
re-sync from this folder later; see the template library's own
Contributing section for how library updates get published).

## Key features

- **Curated template library** spanning vision → requirements → design →
  decisions → governance → operations → migration → validation →
  handover, so a project doesn't start from a blank page.
- **Semantic versioning on every save**, with a full audit trail —
  every revision is attributed to the architect who made it and can't
  be altered or deleted after the fact.
- **Role-scoped governance** — template library changes are restricted
  to `architect`-level users; everyone else can view and use templates
  without being able to alter the shared library.
- **Self-hostable**, packaged as a two-service Docker Compose stack
  (application + PostgreSQL), with no separate reverse proxy required
  by default.

## Getting started

Requires Docker and Docker Compose.

```bash
git clone <this repo>
cd ArchitectureToolkit
cp .env.example .env   # set database credentials
docker compose up
```

The first person to log in is automatically promoted to `architect`,
and the template library is seeded into the database as part of that
same first login — no separate setup step.

## Technology stack

| Layer | Technology |
|---|---|
| Backend | .NET 10, Clean/Onion Architecture (`Application` / `Domain` / `Infrastructure` / `Persistence` / `Presentation.API`) |
| Frontend | Vue 3 + Vuetify |
| Database | PostgreSQL via EF Core |
| Auth | ASP.NET Core Identity + OpenIddict (provider-agnostic OIDC) |
| Packaging | Docker Compose |

## Project structure

```
source/
├── ArchitectureToolkit.Domain/           # Entities, value objects, domain logic
├── ArchitectureToolkit.Application/      # Use cases, commands/queries
├── ArchitectureToolkit.Infrastructure/   # External concerns (auth, etc.)
├── ArchitectureToolkit.Persistence/      # EF Core, DbContexts, migrations
├── ArchitectureToolkit.Presentation.API/ # ASP.NET Core API, serves the SPA
└── ArchitectureToolkit.Tests/            # Includes architecture fitness tests

DocumentationTemplates/                   # The template library (see above)
```

Architecture fitness tests enforce layer isolation directly (e.g. that
`Infrastructure` cannot depend on `Application`, `Domain`,
`Persistence`, or `Presentation`), so the boundaries above are checked,
not just documented.

## License

MIT — see [LICENSE](LICENSE).
