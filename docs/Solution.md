1. Current architecture - clean architecture
    - Alternatives
        - Modular monoliths
        - Vertical slices
        - For more complex applications use eventsourcing
        - If application are to be broken into microservices use event driven architecture

2. Orm - ef core 8



## Deliberate scope omissions

- **Redis, Swagger, GitHub Pages** — cut from `planning.md`'s broader scope; not needed to satisfy the assignment brief.
- **MediatR / pipeline behaviours** — out at the solution root, but opted back in for `Sygnia.Application` specifically (see `src/Sygnia.Backend/src/Sygnia.Application/CLAUDE.md`): commands/queries as records, private sealed handlers, FluentValidation, and a logging pipeline behaviour, registered via an `AppModuleExtensions` DI extension. Scoped to that project rather than solution-wide to keep the dependency out of `Sygnia.Domain` and `Sygnia.Presentation`.