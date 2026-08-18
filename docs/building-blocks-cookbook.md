# BuildingBlocks Cookbook

Every public member of `MediView.BuildingBlocks.*`, with a runnable example for each.
Target: `net10.0`, `Nullable=enable`, `ImplicitUsings=enable`, `TreatWarningsAsErrors=true`.

---

## 0. Where things live

```
src/BuildingBlocks/
├── MediView.BuildingBlocks.Domain/          ← rules of the business
│   ├── IDomainEvent.cs                      "something happened"
│   ├── Entity.cs                            "a thing with an identity"
│   ├── AggregateRoot.cs                     "an entity that records events"
│   └── DomainException.cs                   "a business rule was broken"
└── MediView.BuildingBlocks.Application/     ← orchestration
    └── Result.cs                            "did it work? Error + Result + Result<T>"
```

**Mental model:**

| Layer | Answers | Failure style |
|---|---|---|
| Domain | "Is this legal?" | `throw new DomainException(...)` |
| Application | "Did the use case succeed?" | `return Result.Failure(...)` |

Domain **throws** because a broken invariant is a bug or corruption. Application **returns**
because "email already taken" is an expected outcome, not a crash.

---

# Part 1 — Domain building blocks

## 1.1 `IDomainEvent`

```csharp
public interface IDomainEvent
{
    DateTimeOffset OccurredOn { get; }
}
```

A contract: "any domain event must be able to say **when** it happened."
`DateTimeOffset` (not `DateTime`) because it carries the UTC offset — no timezone guessing later.

### Usage

```csharp
namespace MediView.Identity.Domain.Events;

using MediView.BuildingBlocks.Domain;

public sealed record UserRegisteredDomainEvent(Guid UserId, string Email) : IDomainEvent
{
    public DateTimeOffset OccurredOn { get; } = DateTimeOffset.UtcNow;
}
```

- `record` → free value equality + `ToString()`, handy in tests
- `sealed` → nobody subclasses an event
- `{ get; } = DateTimeOffset.UtcNow;` → **property initializer**; stamped once at construction, then frozen

```csharp
var evt = new UserRegisteredDomainEvent(Guid.NewGuid(), "loi@mediview.dev");
Console.WriteLine(evt.OccurredOn);   // 2026-08-13T09:14:22.113+00:00
Console.WriteLine(evt);              // UserRegisteredDomainEvent { UserId = ..., Email = ... }
```

**Naming:** always past tense. `UserRegistered`, not `RegisterUser`. An event is history — it already happened and cannot be rejected.

---

## 1.2 `Entity<TId>`

```csharp
public abstract class Entity<TId> where TId : notnull
{
    public TId Id { get; protected set; } = default!;

    public override bool Equals(object? obj) =>
        obj is Entity<TId> other && other.GetType() == GetType() && other.Id.Equals(Id);

    public override int GetHashCode() => Id.GetHashCode();
}
```

Syntax notes:

| Piece | Meaning |
|---|---|
| `abstract` | cannot be instantiated directly; only inherited |
| `<TId>` | you choose the id type — `Guid`, `int`, `StudyId`… |
| `where TId : notnull` | a **constraint**: the id type may never be null |
| `{ get; protected set; }` | outsiders read; only this class and children write |
| `= default!` | "start empty, and compiler, stop warning — I'll fill it in" |
| `obj is Entity<TId> other` | **pattern match**: type-check and assign in one step |
| `other.GetType() == GetType()` | a `Doctor` with id 5 ≠ a `Patient` with id 5 |

### Usage — `Id`

```csharp
public sealed class Patient : Entity<Guid>
{
    public string FullName { get; private set; }

    private Patient() { FullName = null!; }        // EF Core needs a parameterless ctor

    public Patient(string fullName)
    {
        Id = Guid.CreateVersion7();                // ← legal: we're inside a derived class
        FullName = fullName;
    }
}
```

`Guid.CreateVersion7()` (.NET 9+) is timestamp-ordered — far better for database index locality
than `Guid.NewGuid()`. Use it for entity ids.

```csharp
var patient = new Patient("Tran Phan Huu Loi");
Console.WriteLine(patient.Id);      // 0192f3c1-....
patient.Id = Guid.Empty;            // ❌ compile error — setter is protected
```

### Usage — `Equals` / `GetHashCode`

Identity equality: two objects are the same **thing** if they share an id, even if
their other fields differ.

```csharp
var a = new Patient("Loi") { };
var b = LoadFromDatabase(a.Id);     // different object in memory, same row

a.Equals(b);                        // ✅ true  — same type, same Id
a == b;                             // ❌ false — see gotcha below

var set = new HashSet<Patient> { a, b };
set.Count;                          // 1 — GetHashCode + Equals agree, so it dedupes
```

> ⚠️ **Gotcha 1 — `==` is not overloaded.** `a == b` still uses *reference* equality, so it
> disagrees with `a.Equals(b)`. Always use `.Equals()` (or `Equals(a, b)`) for entities,
> or add operators to `Entity<TId>`:
> ```csharp
> public static bool operator ==(Entity<TId>? l, Entity<TId>? r) => Equals(l, r);
> public static bool operator !=(Entity<TId>? l, Entity<TId>? r) => !Equals(l, r);
> ```

> ⚠️ **Gotcha 2 — unsaved entities collide.** If `Id` is still `default` (e.g. `Guid.Empty` or `0`)
> for two new objects, they compare **equal** and share a hash code. Assigning the id in the
> constructor (as above) avoids this entirely.

---

## 1.3 `AggregateRoot<TId>`

```csharp
public abstract class AggregateRoot<TId> : Entity<TId> where TId : notnull
{
    private readonly List<IDomainEvent> _domainEvents = new();
    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    protected void Raise(IDomainEvent domainEvent) => _domainEvents.Add(domainEvent);
    public void ClearDomainEvents() => _domainEvents.Clear();
}
```

An **aggregate root** is the only object outside code is allowed to grab. It guards a cluster
of related objects and keeps them consistent. Everything else you reach *through* it.

- `_domainEvents` is `private` — nobody can inject a fake event
- `DomainEvents` returns `IReadOnlyCollection` — callers can look, not add
- `Raise` is `protected` — **only the aggregate itself** may record its own history
- `ClearDomainEvents` is `public` — infrastructure calls it after dispatching

### Usage — the full pattern

```csharp
namespace MediView.Identity.Domain;

using MediView.BuildingBlocks.Domain;
using MediView.Identity.Domain.Events;

public sealed class User : AggregateRoot<Guid>
{
    public string Email { get; private set; }
    public string PasswordHash { get; private set; }
    public bool IsActive { get; private set; }

    private User() { Email = null!; PasswordHash = null!; }   // EF Core

    private User(string email, string passwordHash)
    {
        Id = Guid.CreateVersion7();
        Email = email;
        PasswordHash = passwordHash;
        IsActive = true;
    }

    // Named factory — clearer than a bare constructor, and it can enforce rules.
    public static User Register(string email, string passwordHash)
    {
        if (string.IsNullOrWhiteSpace(email))
            throw new DomainException("Email is required.");

        var user = new User(email, passwordHash);
        user.Raise(new UserRegisteredDomainEvent(user.Id, user.Email));   // ← Raise
        return user;
    }

    public void Deactivate(string reason)
    {
        if (!IsActive)
            throw new DomainException("User is already deactivated.");

        IsActive = false;
        Raise(new UserDeactivatedDomainEvent(Id, reason, DateTimeOffset.UtcNow));
    }
}
```

### Usage — `DomainEvents` (reading)

```csharp
var user = User.Register("loi@mediview.dev", hash);

user.DomainEvents.Count;              // 1
user.DomainEvents.First();            // UserRegisteredDomainEvent { ... }

user.DomainEvents.Add(somethingElse); // ❌ compile error — IReadOnlyCollection has no Add
user.Raise(somethingElse);            // ❌ compile error — Raise is protected
```

That double lock is the whole point: an aggregate's history can only be written by the
aggregate, in the same method that changed the state.

### Usage — `ClearDomainEvents` (infrastructure)

Events are collected during the business operation, then dispatched **once, after** the
database transaction commits. Otherwise you email a user about a registration that got rolled back.

```csharp
public sealed class IdentityDbContext(DbContextOptions<IdentityDbContext> options)
    : DbContext(options)
{
    public DbSet<User> Users => Set<User>();

    public async Task<int> SaveChangesAndDispatchAsync(
        IDomainEventDispatcher dispatcher,
        CancellationToken ct = default)
    {
        // 1. Harvest events from every tracked aggregate
        var aggregates = ChangeTracker
            .Entries<AggregateRoot<Guid>>()
            .Where(e => e.Entity.DomainEvents.Count > 0)
            .Select(e => e.Entity)
            .ToList();

        var events = aggregates.SelectMany(a => a.DomainEvents).ToList();

        // 2. Clear BEFORE dispatch, so a handler that saves again can't re-fire them
        foreach (var aggregate in aggregates)
            aggregate.ClearDomainEvents();                                // ← ClearDomainEvents

        // 3. Commit, then publish
        var affected = await SaveChangesAsync(ct);

        foreach (var domainEvent in events)
            await dispatcher.DispatchAsync(domainEvent, ct);

        return affected;
    }
}
```

> ⚠️ **Gotcha 3 — `AsReadOnly()` allocates.** `DomainEvents` builds a new wrapper object on
> *every* access. Harmless here, but don't call it in a tight loop. Cache it in a local first,
> as the harvest code above does.

---

## 1.4 `DomainException`

```csharp
public class DomainException(string message) : Exception(message);
```

Three C# 12 features in one line:

1. **Primary constructor on a class** — `(string message)` generates the constructor
2. **Base call in the header** — `: Exception(message)` forwards the argument up
3. **Semicolon body** — no members, so no `{ }` needed at all

The long form is 8 lines. This is the same thing.

### Usage — throwing

```csharp
public void Cancel()
{
    if (Status == StudyStatus.Completed)
        throw new DomainException("A completed study cannot be cancelled.");

    Status = StudyStatus.Cancelled;
}
```

### Usage — catching at the edge

Map it to an HTTP status so business-rule violations become `400`, not `500`:

```csharp
app.UseExceptionHandler(handler => handler.Run(async context =>
{
    var ex = context.Features.Get<IExceptionHandlerFeature>()?.Error;

    var (status, title) = ex switch
    {
        DomainException => (StatusCodes.Status400BadRequest, ex.Message),
        _               => (StatusCodes.Status500InternalServerError, "Unexpected error."),
    };

    context.Response.StatusCode = status;
    await context.Response.WriteAsJsonAsync(new ProblemDetails { Status = status, Title = title });
}));
```

**When to use which:**

| Situation | Use |
|---|---|
| "Completed study can't be cancelled" — impossible state | `DomainException` |
| "Email already registered" — expected, user-fixable | `Result.Failure` |
| "Database is down" | let it bubble up |

---

# Part 2 — Application building blocks

## 2.1 `Error`

```csharp
public sealed record Error(string Code, string Message)
{
    public static readonly Error None = new(string.Empty, string.Empty);
}
```

### Usage — creating

```csharp
var notFound = new Error("User.NotFound", "No user with that id.");

notFound.Code;      // "User.NotFound"    ← for machines: logs, i18n keys, client switches
notFound.Message;   // "No user with..."  ← for humans
```

**Best practice — a static catalogue.** Never scatter error strings across handlers:

```csharp
namespace MediView.Identity.Application;

using MediView.BuildingBlocks.Application;

public static class UserErrors
{
    public static readonly Error NotFound =
        new("User.NotFound", "The user was not found.");

    public static readonly Error EmailTaken =
        new("User.EmailTaken", "That email is already registered.");

    // A method when the message needs data baked in:
    public static Error InvalidEmail(string email) =>
        new("User.InvalidEmail", $"'{email}' is not a valid email address.");
}
```

```csharp
return Result.Failure<UserDto>(UserErrors.NotFound);
return Result.Failure<UserDto>(UserErrors.InvalidEmail(request.Email));
```

### Usage — `Error.None`

The single shared "nothing went wrong" sticker. It exists so a success never stores `null`.

```csharp
Error.None.Code;                        // ""
Result.Success().Error == Error.None;   // true
```

Because `Error` is a `record`, `==` compares **contents**. So any blank error equals `None`:

```csharp
new Error("", "") == Error.None;        // true  ← value equality
ReferenceEquals(new Error("", ""), Error.None);  // false — different objects
```

That is exactly why the `Result` constructor's guard works.

---

## 2.2 `Result` — the non-generic box

```csharp
public class Result
{
    protected Result(bool isSuccess, Error error) { /* guard, then assign */ }

    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;
    public Error Error { get; }

    public static Result Success() => new(true, Error.None);
    public static Result Failure(Error error) => new(false, error);
    public static Result<T> Success<T>(T value) => new(value, true, Error.None);
    public static Result<T> Failure<T>(Error error) => new(default, false, error);
}
```

Use `Result` when the operation has **nothing to hand back** — delete, activate, send.

### `Result.Success()`

```csharp
public async Task<Result> DeactivateAsync(Guid userId, CancellationToken ct)
{
    var user = await _db.Users.FindAsync([userId], ct);
    if (user is null)
        return Result.Failure(UserErrors.NotFound);

    user.Deactivate("Requested by admin");
    await _db.SaveChangesAsync(ct);

    return Result.Success();          // ← "it worked, nothing to return"
}
```

### `Result.Failure(Error)`

```csharp
var result = Result.Failure(UserErrors.EmailTaken);

result.IsSuccess;       // false
result.IsFailure;       // true
result.Error.Code;      // "User.EmailTaken"
```

### `Result.Success<T>(T value)`

`T` is **inferred** from the argument — you never write it out:

```csharp
Result<int>    a = Result.Success(42);            // T = int
Result<string> b = Result.Success("hello");       // T = string
Result<User>   c = Result.Success(user);          // T = User
```

### `Result.Failure<T>(Error)`

Here `T` **must be written explicitly**. There's no value to infer it from — the only
argument is an `Error`, which says nothing about `T`:

```csharp
Result<User> r = Result.Failure<User>(UserErrors.NotFound);   // ✅
Result<User> r = Result.Failure(UserErrors.NotFound);         // ❌ returns Result, not Result<User>
```

This is the single most common mistake with this type. **Failure needs the `<T>`; Success doesn't.**

### `IsSuccess` / `IsFailure` / `Error`

```csharp
var result = await _handler.HandleAsync(command, ct);

if (result.IsFailure)
{
    _logger.LogWarning("Failed: {Code} {Message}", result.Error.Code, result.Error.Message);
    return Results.BadRequest(new { result.Error.Code, result.Error.Message });
}

return Results.Ok();
```

`IsFailure` is a **computed** property (`=> !IsSuccess`) — nothing stored, evaluated on each read.
It exists purely so `if (result.IsFailure)` reads better than `if (!result.IsSuccess)`.

### The constructor guard

```csharp
protected Result(bool isSuccess, Error error)
{
    if (isSuccess && error != Error.None)
        throw new InvalidOperationException();
    ...
}
```

`protected` means **you cannot call `new Result(...)` from outside**. The factory methods are the
only door. This makes an incoherent result — "I succeeded, and here's the error" — unrepresentable.

---

## 2.3 `Result<T>` — the box with something inside

```csharp
public class Result<T> : Result
{
    private readonly T? _value;
    protected internal Result(T? value, bool isSuccess, Error error) : base(isSuccess, error)
        => _value = value;

    public T Value => IsSuccess ? _value! : throw new InvalidOperationException("No value on a failure.");
    public static implicit operator Result<T>(T value) => Success(value);
}
```

Because it inherits (`: Result`), it **already has** `IsSuccess`, `IsFailure`, and `Error`.

### `Value`

```csharp
var ok = Result.Success(new UserDto("loi@mediview.dev"));
ok.Value.Email;                       // ✅ "loi@mediview.dev"

var bad = Result.Failure<UserDto>(UserErrors.NotFound);
bad.Value;                            // 💥 InvalidOperationException: No value on a failure.
```

**Always check first.** `Value` deliberately explodes rather than handing back a silent `null`:

```csharp
if (result.IsSuccess)
    Console.WriteLine(result.Value.Email);
```

### The implicit operator

Lets a bare value auto-convert into a successful result:

```csharp
public Result<UserDto> Map(User user)
{
    return new UserDto(user.Email);       // ← no Result.Success(...) needed
}
```

```csharp
Result<int> r = 42;        // identical to Result.Success(42)
r.IsSuccess;               // true
r.Value;                   // 42
```

> ⚠️ **Gotcha 4 — `null` sneaks through.** `Result<User> r = null;` compiles and produces a
> **success holding null**, and `r.Value` then returns that null despite the `!` promise. Guard it:
> ```csharp
> public static implicit operator Result<T>(T value) =>
>     value is null
>         ? Failure<T>(new Error("Value.Null", "Value cannot be null."))
>         : Success(value);
> ```

---

# Part 3 — Everything together

A realistic Identity use case, top to bottom:

```csharp
// ── Application layer ──────────────────────────────────────────────
namespace MediView.Identity.Application.Users;

using MediView.BuildingBlocks.Application;
using MediView.BuildingBlocks.Domain;
using MediView.Identity.Domain;

public sealed record RegisterUserCommand(string Email, string Password);
public sealed record UserDto(Guid Id, string Email);

public sealed class RegisterUserHandler(IdentityDbContext db, IDomainEventDispatcher dispatcher)
{
    public async Task<Result<UserDto>> HandleAsync(RegisterUserCommand command, CancellationToken ct)
    {
        // 1. Expected failure → Result (note the explicit <UserDto>)
        if (!command.Email.Contains('@'))
            return Result.Failure<UserDto>(UserErrors.InvalidEmail(command.Email));

        var exists = await db.Users.AnyAsync(u => u.Email == command.Email, ct);
        if (exists)
            return Result.Failure<UserDto>(UserErrors.EmailTaken);

        // 2. Domain does the work and records its own history via Raise(...)
        //    If an invariant breaks here, it throws DomainException — that's a bug, not a flow.
        var user = User.Register(command.Email, BCrypt.Net.BCrypt.HashPassword(command.Password));

        // 3. Persist, then dispatch events (which also calls ClearDomainEvents)
        db.Users.Add(user);
        await db.SaveChangesAndDispatchAsync(dispatcher, ct);

        // 4. Success — implicit operator wraps the DTO for us
        return new UserDto(user.Id, user.Email);
    }
}

// ── API layer ──────────────────────────────────────────────────────
app.MapPost("/users/register", async (
    RegisterUserCommand command,
    RegisterUserHandler handler,
    CancellationToken ct) =>
{
    var result = await handler.HandleAsync(command, ct);

    return result.IsSuccess
        ? Results.Created($"/users/{result.Value.Id}", result.Value)
        : Results.BadRequest(new { result.Error.Code, result.Error.Message });
});
```

---

# Part 4 — Cheat sheet

| I want to… | Write |
|---|---|
| Say "it worked", nothing to return | `return Result.Success();` |
| Say "it worked", here's the data | `return Result.Success(dto);` or just `return dto;` |
| Say "it failed", no data expected | `return Result.Failure(UserErrors.NotFound);` |
| Say "it failed", data was expected | `return Result.Failure<UserDto>(UserErrors.NotFound);` ← **needs `<T>`** |
| Check the outcome | `if (result.IsFailure) { … }` |
| Read the payload | `result.Value` — **only after** checking `IsSuccess` |
| Read the reason | `result.Error.Code` / `result.Error.Message` |
| Give an entity an id | `Id = Guid.CreateVersion7();` inside the constructor |
| Record that something happened | `Raise(new XyzDomainEvent(...));` inside the aggregate |
| Read an aggregate's events | `aggregate.DomainEvents` (read-only) |
| Wipe events after dispatch | `aggregate.ClearDomainEvents();` |
| Reject an impossible state | `throw new DomainException("…");` |

## Syntax glossary

| Syntax | Name | Means |
|---|---|---|
| `namespace X;` | file-scoped namespace | everything in this file lives in `X` |
| `=> expr` | expression body | one-line method/property, computed each call |
| `new(...)` | target-typed new | type comes from the left-hand side |
| `record` | record type | compared by contents, not identity |
| `sealed` | sealed | nobody may inherit from it |
| `abstract` | abstract | can only be inherited, never instantiated |
| `: Base` | inheritance | "is a Base, plus more" |
| `: base(x)` | base ctor call | run the parent's constructor first |
| `<T>` | generic | a placeholder for any type |
| `where T : notnull` | constraint | T can never be null |
| `T?` | nullable | might be missing |
| `x!` | null-forgiving | "trust me, not null" — compile-time only |
| `a ? b : c` | ternary | if a then b else c |
| `{ get; }` | read-only property | set once in the constructor, then frozen |
| `obj is X other` | pattern match | type-test and bind in one step |
| `(string Code)` | primary constructor | generates ctor + members from the header |

---

# Part 5 — Open items in this repo

1. **No service references the building blocks yet.** Each service references only its own
   Domain/Application/Infrastructure. Before using any of the above, add:
   ```bash
   dotnet add src/Services/Identity/MediView.Identity.Domain \
     reference src/BuildingBlocks/MediView.BuildingBlocks.Domain
   dotnet add src/Services/Identity/MediView.Identity.Application \
     reference src/BuildingBlocks/MediView.BuildingBlocks.Application
   ```
   Neither BuildingBlocks project is in the `.sln` either — `dotnet sln add` them.

2. **`Class1.cs` placeholders** still sit in all 12 service Domain/Application/Infrastructure
   projects. Delete as you fill each one in.

3. **`Result` failure guard is one-sided.** `Result.Failure(Error.None)` is currently legal and
   produces a failure with a blank message. Add the mirror check:
   ```csharp
   if (!isSuccess && error == Error.None)
       throw new InvalidOperationException("A failed result must carry an error.");
   ```

4. **`IDomainEventDispatcher` does not exist yet** — the `SaveChangesAndDispatchAsync` example
   above assumes it. It's the next building block to write.

5. `TreatWarningsAsErrors=true` is on. Nullable warnings will **break your build**, not nag you.
   That's why `= default!` and `_value!` appear in the building blocks.
