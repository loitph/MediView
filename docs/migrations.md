# Database migrations

Each service owns one Postgres database and one schema, and keeps its EF Core migrations in
`MediView.<Service>.Infrastructure/Migrations/`. The examples below use Identity; swap the
service name for Studies, Imaging or Reporting.

## How they are applied

You never apply migrations by hand in day-to-day work. `run.sh` starts every service with the
`http` launch profile, which sets `ASPNETCORE_ENVIRONMENT=Development`, and in Development each
service calls `Database.Migrate()` on startup:

```csharp
if (app.Environment.IsDevelopment())
{
    app.Services.MigrateIdentityDatabase();
}
```

On startup the service compares the files in `Migrations/` with the `__ef_migrations_history`
table in its schema and runs only the ones the database has not seen.

```
change an entity  ->  dotnet ef migrations add <Name>  ->  ./run.sh  ->  commit both
                      (you, once)                          (applies it)
```

## One-time setup on a new machine

```bash
dotnet tool install -g dotnet-ef
```

If it is already installed, `dotnet tool update -g dotnet-ef` brings it level with the EF Core
runtime and silences the "tools version is older" warning.

Then follow the README: set the user-secrets connection strings and run `./run.sh`. A fresh
database gets every migration on the first start.

## Changing an entity: adding `PhoneNumber` to `User`

1. Add the property to `MediView.Identity.Domain/Users/User.cs`. Make it nullable, because rows
   that already exist have no value:

   ```csharp
   public string? PhoneNumber { get; private set; }
   ```

2. Map it in `MediView.Identity.Infrastructure/Persistence/Configurations/UserConfiguration.cs`:

   ```csharp
   builder.Property(user => user.PhoneNumber).HasMaxLength(32);
   ```

3. Generate the migration. Name it after the change, in PascalCase:

   ```bash
   dotnet ef migrations add AddUserPhoneNumber \
     -p src/Services/Identity/MediView.Identity.Infrastructure \
     -s src/Services/Identity/MediView.Identity.Api \
     -o Migrations
   ```

   EF compares the model with `IdentityDbContextModelSnapshot.cs` and writes only the
   difference. Open the new file and check it does what you expect:

   ```csharp
   migrationBuilder.AddColumn<string>(
       name: "phone_number", schema: "identity", table: "users",
       type: "character varying(32)", maxLength: 32, nullable: true);
   ```

4. Apply it with `./run.sh`, or without starting the app:

   ```bash
   dotnet ef database update \
     -p src/Services/Identity/MediView.Identity.Infrastructure \
     -s src/Services/Identity/MediView.Identity.Api
   ```

5. Check the column exists:

   ```bash
   docker compose exec postgres psql -U mediview -d mediview_identity -c '\d identity.users'
   ```

6. Commit the entity, the configuration and the three changed files in `Migrations/`
   (the migration, its `.Designer.cs` and the snapshot) together.

`-p` is the project that holds the migrations (Infrastructure). `-s` is the startup project,
which supplies the user-secrets connection string (Api).

## Command reference

Run from the repository root. `…` stands for the `-p`/`-s` pair above.

| Goal | Command |
| --- | --- |
| Create a migration | `dotnet ef migrations add <Name> … -o Migrations` |
| Apply pending migrations | `dotnet ef database update …` |
| List migrations and which are applied | `dotnet ef migrations list …` |
| Preview the SQL | `dotnet ef migrations script … --idempotent` |
| Undo the last migration, not yet applied | `dotnet ef migrations remove …` |
| Undo the last migration, already applied locally | `dotnet ef database update <PreviousName> …` then `dotnet ef migrations remove …` |

## Rules

- **Never edit or delete a migration that has been pushed.** Other machines have already run it.
  Fix mistakes with a new migration.
- **One migration per change**, named after what it does (`AddUserPhoneNumber`, not `Update2`).
- **New columns on existing tables are nullable or have a default.** Otherwise the migration fails
  on a database that already has rows.
- **Read the generated file before committing.** A rename can come out as drop + add, which loses
  data; rewrite it as `RenameColumn` when that happens.
- Files in `Migrations/` are generated. `.editorconfig` marks them `generated_code = true`, so the
  analyzers and the no-comments rule skip them.

## When a migration fails

The service stops during startup, and the Postgres error is printed to the console and sent to
Seq (http://localhost:5341). The usual causes are a new unique index on duplicate data, or a
non-nullable column added to a table that already has rows.

While the work is only on your machine, you can start again from an empty database:

```bash
docker compose down -v
./run.sh
```

`down -v` deletes the volumes, so every service's data is gone, not only the one you were
changing.
