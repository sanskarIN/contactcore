# ADR 0001: Use a Layered Modular Monolith

- **Status:** Accepted
- **Scope:** Repository/application architecture
- **Decision owners:** ContactCore maintainers

## Context

ContactCore is a cross-platform desktop contact manager whose primary responsibilities are local UI, contact-domain rules, interchange workflows, and a local SQLite database. It does not require a server process or independent deployment units.

The project still needs strong separation so:

- domain logic can be tested without Avalonia or SQLite;
- use cases can be expressed against interfaces;
- persistence can change without rewriting domain models;
- UI/platform APIs do not leak into storage/business rules;
- tests can target each layer independently;
- a contributor can understand dependency direction quickly.

Splitting these concerns into networked microservices would add process orchestration, APIs, authentication/transport choices, failure modes, latency, packaging complexity, and a larger privacy/security surface without a current product requirement for independent scaling/deployment.

## Decision

Use one repository/application deployment organized as a layered modular monolith with four production projects:

1. `ContactCore.Domain`
2. `ContactCore.Application`
3. `ContactCore.Infrastructure`
4. `ContactCore.Desktop`

Dependency direction is inward:

```text
Desktop ──► Application ──► Domain
   │              ▲
   └──► Infrastructure ──┘
             │
             └──────────► Domain
```

Application owns abstractions such as `IContactRepository`, `IBackupService`, and `IAppPreferences`. Infrastructure implements them. Desktop owns composition and platform presentation.

Each production layer has a corresponding test project.

## Layer responsibilities

### Domain

Pure contact model, value records, validation, normalization, display/deep-copy behavior. No Avalonia, SQLite, filesystem, or application-service dependencies.

### Application

Use-case orchestration, contracts, duplicate/merge policy, and CSV/vCard codecs. It can depend on Domain but should not depend on concrete SQLite/Avalonia implementations.

### Infrastructure

SQLite, migrations, local filesystem paths, backup/restore, preferences serialization, redaction, and optional keyed-SQLite integration. Implements Application abstractions.

### Desktop

Avalonia startup/views/styles/view models plus native file pickers, dialogs, keyboard handling, and concrete dependency composition.

## Consequences

### Positive

- Simple single-app deployment.
- Offline-first behavior does not need internal network calls.
- Compile-time project references reinforce dependency rules.
- Domain/Application tests run without launching the UI.
- SQLite/native complexity is isolated.
- UI can evolve independently from persistence internals.
- Fewer operational/security surfaces than service decomposition.

### Negative

- All modules are released together.
- No independent horizontal scaling of individual modules (not currently needed).
- Architecture discipline still depends on reviewers avoiding cross-layer convenience dependencies.
- Desktop currently references Infrastructure because it is the composition root; careless UI code could bypass Application if review rules are ignored.

## Alternatives considered

### Single project

Rejected because Avalonia, SQLite, models, use cases, and tests would be too easy to couple and harder to reason about.

### Microservices/server API

Rejected for the current product because it would undermine the simplest local/offline deployment and add unnecessary network/security/operations complexity.

### Plugin-first architecture

Deferred. ContactCore currently has no proven requirement for third-party runtime extensions. A plugin boundary would introduce code-loading/security/versioning concerns prematurely.

### MVVM framework/service container everywhere

Not required as an architecture choice. CommunityToolkit.Mvvm is used for presentation convenience; explicit construction in `App.axaml.cs` keeps the composition understandable without requiring a DI container.

## Guardrails

A change violates this ADR if it:

- makes Domain reference Avalonia/SQLite;
- makes Application depend on concrete Infrastructure implementation;
- puts core database access directly throughout Desktop instead of through the Application contract for convenience;
- introduces an internal network service without an explicit new architecture decision.

## When to revisit

Revisit if ContactCore gains a concrete requirement for independently deployed services, multi-user/network synchronization, third-party plugin isolation, or another capability that cannot be cleanly handled within the current process/module boundaries.

Any replacement should include a migration plan preserving offline/privacy/data-integrity guarantees.
