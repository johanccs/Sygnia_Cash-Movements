** Context
1. The Sygnia.Application project is used to contain the business logic of the application.
2.  Busines rules are:
    - Concurrency: submissions may arrive at the same time from multiple callers.
    - Uniqueness: externalRef must be unique per account - can be handled at the database layer
    - Re-submissions of the same pair (accountId, externalRef) must
not double-count. - Should account for idempotency and handled in Sygnia.Application
    - Field conflicts: if a repeated externalRef for the same account differs from the original fields (for example, amount or currency), do not double-count; choose how to surface this and explain your choice - ** do not ** continue with the transaction. Write to 