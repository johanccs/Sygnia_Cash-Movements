** Context
1. The Sygnia.Application project is used to contain the business logic of the application.
2.  Busines rules are:
    - Concurrency: submissions may arrive at the same time from multiple callers.
    - Uniqueness: externalRef must be unique per account - can be handled at the database layer
    - Re-submissions of the same pair (accountId, externalRef) must
not double-count. - Should account for idempotency and handled in Sygnia.Application
    - Field conflicts: if a repeated externalRef for the same account differs from the original fields (for example, amount or currency), do not double-count; choose how to surface this and explain your choice - ** do not ** continue with the transaction. Write to a log file, consider writing to an audit table. Return a Result class with an error message to the end-user.
    - New and legacy entry points (if implemented) should produce consistent outcomes over the same data.
3. Use cases:
    **In spec:**
    - Movement: a single cash movement for an account with fields:
            • externalRef: example MOV-20240715-000123
            • accountId: example ACC-001
            • currency: example ZAR
            • amount: example 12500.00 (positive for deposit, negative for withdrawal)
            • occurredAt: example 2024-07-15T10:42:31Z
            • narration: example Initial deposit
    - Balance: the sum of movements for an account
    - Statement: an ordered list of movements in a date range with a running total

    **Out of spec**
    - Create an account  - src\Sygnia.Backend\src\Sygnia.Domain\Models\Account.cs
    - Create a user - src\Sygnia.Backend\src\Sygnia.Domain\Models\User.cs

** Instructions
0. Read the original first-draft.md and ensure business rules are still in sync.
1. Implement business rules by using mediatr library
    - Create folders for the commands