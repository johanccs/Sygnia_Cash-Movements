1. Current architecture - clean architecture
   - Alternatives
     - Modular monoliths
     - Vertical slices
     - For more complex applications use eventsourcing
     - If application are to be broken into microservices use event driven architecture

2. Orm - ef core 8

**Things to consider**
3. As application evolve - next steps is to ensure that there is a maker/checker procedure in the movement use case. This is to ensure the the person which initiated the move cannot approve it. We need a second person to aprove.
4. Currently the db details are in appsettings.json for simplicity but in real world project the developing locally would make use of application secrets and not in appsettings. The credentials will be overwritten in deployment pipelines with values in azure devops or aws secret manager. This can also be rotated on weekly basis to improve security.

## Deliberate scope omissions

- **Redis, Swagger, GitHub Pages** — cut from `planning.md`'s broader scope; not needed to satisfy the assignment brief.
- **MediatR / pipeline behaviours** — out at the solution root, but opted back in for `Sygnia.Application` specifically (see `src/Sygnia.Backend/src/Sygnia.Application/CLAUDE.md`): commands/queries as records, private sealed handlers, FluentValidation, and a logging pipeline behaviour, registered via an `AppModuleExtensions` DI extension. Scoped to that project rather than solution-wide to keep the dependency out of `Sygnia.Domain` and `Sygnia.Presentation`.
