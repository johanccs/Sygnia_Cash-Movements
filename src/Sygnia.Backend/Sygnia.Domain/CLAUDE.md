# Sygnia.Domain

Guidance for working in this project. The root `CLAUDE.md` still applies; this adds what is
specific to the domain layer.

## Original instructions (complete)

```
** Instructions
1. Create Models folder in src\Sygnia.Backend\Sygnia.Domain project
2. Create domain models:
    - Account
    - User
    - Movement
3. Create methods to update account name and contactperson

```

All three are implemented and covered by tests in
`src/Sygnia.Backend/tests/Sygnia.UnitTests/`.

## What is here

```
Sygnia.Domain/
├─ Models/
│   ├─ Account.cs    AccountId, AccountName, ContactPerson, CreatedDate, CreatedBy
│   │                + WithAccountName / WithContactPerson
│   ├─ Movement.cs   the (AccountId, ExternalRef) composite key; signed Amount; IsDeposit
│   └─ User.cs       Id, Name, Surname, FullName
├─ Guard.cs          shared guard clauses (internal)
├─ Result.cs         Result<T> — expected failures as values
└─ Error.cs          Error(Code, Message)
```

**This project references nothing.** It is the innermost layer; adding a dependency here —
EF Core, a NuGet package, another project — breaks Clean Architecture. If something seems to
be needed from outside, the type belongs in `Sygnia.Application` instead.

## Rules that bind this layer

**Constructors throw; methods return `Result<T>`.** Not a contradiction — they split failures
in two. A `Movement` with a null account id is a broken invariant, an object that must never
exist, so the constructor throws and the type system then guarantees every `Movement` in the
system is valid. A name that is too long is an expected outcome the caller must handle, so it
comes back as a `Result`. Constructors use `Guard`; methods return `Result<T>.Failure`.

**Every guard runs before any field is assigned**, so a rejected object is never half-built.

**Properties are readonly and set once, in the constructor.** There are no setters, so an
"update" returns a *new* instance and leaves the original untouched — see
`Account.WithAccountName`. Any new update method follows that shape.

**Classes are `sealed`.**

## Specifics that are easy to get wrong

- **`Amount` must be non-zero.** Sign carries meaning: positive is a deposit, negative a
  withdrawal. Zero is not a small movement, it is an absent one.
- **`DateTime` must be UTC**, not merely non-default. A `Local` or `Unspecified` value would
  be persisted as though it were UTC — silent at write time, near-untraceable afterwards.
  `OccurredAt`, `MovedDate` and `CreatedDate` all require `DateTimeKind.Utc`.
- **Length limits mirror the SQL columns exactly** (`AccountId` 10, `ExternalRef` 20,
  `AccountName` 20, `ContactPerson` 30, `Narration` 200, user ids 50). They are `public const`
  on each model — reuse them rather than retyping the number, so the model and the schema
  cannot drift apart.
- **`Error.Code` is machine-readable and stable** (`account.name.invalid`). The transport layer
  maps it to a gRPC status, so do not reword a code casually; the message is the free part.
- **Reading `Result.Value` on a failure throws**, as does reading `Result.Error` on a success.
  Both are caller bugs rather than expected outcomes, so they are loud by design.

## Testing

TDD is binding here. Write the failing test, watch it fail *for the right reason*, then
implement. In practice that means adding the test against an unvalidated stub so it fails on
behaviour — a compile error is a weak red that proves nothing about the guard.

```bash
dotnet test    # from src/Sygnia.Backend
```

## Known gap

`Currency` is validated as three letters but **not** normalised to uppercase, so `"zar"` and
`"ZAR"` would both be accepted as distinct values. Left unnormalised because no test covers
it; worth deciding before movements are persisted.
