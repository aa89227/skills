---
name: aspire-atlas-local
description: |
  Custom Aspire resource for MongoDB Atlas Local container with Atlas Search / Vector Search support.
  Use when creating or reviewing Aspire AppHost projects that need MongoDB Atlas Local
  (`mongodb/mongodb-atlas-local`) with custom resource definitions, database sub-resources,
  volume mounts, and connection string expressions.
  Trigger phrases: "Aspire MongoDB", "Atlas Local Aspire", "MongoDbAtlasLocal",
  "AddMongoDbAtlasLocal", "Aspire custom resource", "atlas-local resource",
  "MongoDB Aspire container", "Aspire mongo setup".
license: MIT
metadata:
  author: aa89227
  version: "1.0"
  tags: ["aspire", "mongodb", "atlas-local", "container", "custom-resource", "atlas-search"]
---

# Aspire Custom Resource: MongoDB Atlas Local

> Custom Aspire resource for `mongodb/mongodb-atlas-local` — supports `$search` and `$vectorSearch` out of the box.

## Quick Reference

| Item | Value |
|---|---|
| Image | `mongodb/mongodb-atlas-local` |
| Recommended tag | `8.2.4` |
| Container port | `27017` |
| Auth env vars | `MONGODB_INITDB_ROOT_USERNAME`, `MONGODB_INITDB_ROOT_PASSWORD` |
| Connection string | `mongodb://{user}:{pass}@{host}:{port}` |
| Database connection | `{parent}/{dbName}?directConnection=true&authSource=admin` |

## Core Rules

- Atlas Local uses `MONGODB_INITDB_ROOT_USERNAME` / `MONGODB_INITDB_ROOT_PASSWORD` (note the extra **DB** — standard `mongo` image uses `MONGO_INITDB_ROOT_*`).
- Set `HOSTNAME` env var **and** `--hostname` container runtime arg for stable replica set config across restarts.
- Use `ContainerLifetime.Persistent` for development — Atlas Local is slow to start.
- Database sub-resource connection string **must** include `?directConnection=true&authSource=admin`.
- Password follows Aspire convention — auto-generated via `CreateDefaultPasswordParameter` when not provided.

## Resource: MongoDbAtlasLocalResource

```csharp
public sealed class MongoDbAtlasLocalResource(
    string name,
    ParameterResource? userName,
    ParameterResource password)
    : ContainerResource(name), IResourceWithConnectionString
{
    internal const string PrimaryEndpointName = "mongodb";
    private const string DefaultUserName = "admin";

    private EndpointReference? _primaryEndpoint;
    public EndpointReference PrimaryEndpoint =>
        _primaryEndpoint ??= new(this, PrimaryEndpointName);

    internal ParameterResource? UserNameParameter { get; } = userName;
    internal ParameterResource PasswordParameter { get; } = password;

    private ReferenceExpression UserNameReference =>
        userName is not null
            ? ReferenceExpression.Create($"{userName}")
            : ReferenceExpression.Create($"{DefaultUserName}");

    public ReferenceExpression ConnectionStringExpression =>
        ReferenceExpression.Create(
            $"mongodb://{UserNameReference}:{PasswordParameter}@{PrimaryEndpoint.Property(EndpointProperty.Host)}:{PrimaryEndpoint.Property(EndpointProperty.Port)}");
}
```

Key points:
- Primary constructor with `name`, `userName` (nullable), `password`.
- `UserNameReference` falls back to `"admin"` when `userName` is null.
- `ConnectionStringExpression` builds the full `mongodb://` URI using Aspire's `ReferenceExpression`.

## Resource: MongoDbAtlasLocalDatabaseResource

```csharp
public sealed class MongoDbAtlasLocalDatabaseResource(
    string name,
    string databaseName,
    MongoDbAtlasLocalResource parent)
    : Resource(name), IResourceWithConnectionString,
        IResourceWithParent<MongoDbAtlasLocalResource>
{
    public MongoDbAtlasLocalResource Parent => parent;
    public string DatabaseName { get; } = databaseName;

    public ReferenceExpression ConnectionStringExpression =>
        ReferenceExpression.Create(
            $"{Parent}/{DatabaseName}?directConnection=true&authSource=admin");
}
```

Key points:
- Implements `IResourceWithParent<MongoDbAtlasLocalResource>` — inherits parent connection string.
- `directConnection=true` — required for single-node replica set.
- `authSource=admin` — Atlas Local stores credentials in admin database.

## Extensions

### AddMongoDbAtlasLocal

```csharp
public static IResourceBuilder<MongoDbAtlasLocalResource> AddMongoDbAtlasLocal(
    this IDistributedApplicationBuilder builder,
    string name,
    int? port = null,
    IResourceBuilder<ParameterResource>? userName = null,
    IResourceBuilder<ParameterResource>? password = null,
    IResourceBuilder<ParameterResource>? hostname = null)
{
    // Read hostname from parameter or config fallback
    var hostnameValue = hostname is not null
        ? hostname.Resource.Value
        : builder.Configuration["MONGO_ATLAS_HOSTNAME"];

    // Auto-generate password if not provided (Aspire convention)
    var passwordParameter = password?.Resource
        ?? ParameterResourceBuilderExtensions.CreateDefaultPasswordParameter(
            builder, $"{name}-password", special: false);

    var resource = new MongoDbAtlasLocalResource(
        name, userName?.Resource, passwordParameter);

    var rb = builder.AddResource(resource)
        .WithImage("mongodb/mongodb-atlas-local")
        .WithImageTag("8.2.4")
        .WithEndpoint(port: port, targetPort: 27017,
            name: MongoDbAtlasLocalResource.PrimaryEndpointName)
        .WithEnvironment(context =>
        {
            context.EnvironmentVariables["MONGODB_INITDB_ROOT_USERNAME"] =
                resource.UserNameParameter is not null
                    ? ReferenceExpression.Create($"{resource.UserNameParameter}")
                    : ReferenceExpression.Create($"admin");
            context.EnvironmentVariables["MONGODB_INITDB_ROOT_PASSWORD"] =
                ReferenceExpression.Create($"{resource.PasswordParameter}");

            if (hostnameValue is not null)
                context.EnvironmentVariables["HOSTNAME"] = hostnameValue;
        });

    if (hostnameValue is not null)
        rb = rb.WithContainerRuntimeArgs("--hostname", hostnameValue);

    return rb;
}
```

### AddDatabase

```csharp
public static IResourceBuilder<MongoDbAtlasLocalDatabaseResource> AddDatabase(
    this IResourceBuilder<MongoDbAtlasLocalResource> builder,
    string name,
    string? databaseName = null)
{
    var dbName = databaseName ?? name;
    var resource = new MongoDbAtlasLocalDatabaseResource(
        name, dbName, builder.Resource);
    return builder.ApplicationBuilder.AddResource(resource);
}
```

### WithAtlasDataVolume / WithSearchVolume

```csharp
public static IResourceBuilder<MongoDbAtlasLocalResource> WithAtlasDataVolume(
    this IResourceBuilder<MongoDbAtlasLocalResource> builder,
    string? name = null)
{
    return builder
        .WithVolume(name ?? "atlas-data", "/data/db")
        .WithVolume((name ?? "atlas") + "-configdb", "/data/configdb");
}

public static IResourceBuilder<MongoDbAtlasLocalResource> WithSearchVolume(
    this IResourceBuilder<MongoDbAtlasLocalResource> builder,
    string? name = null)
{
    return builder.WithVolume(name ?? "atlas-mongot", "/data/mongot");
}
```

## Usage in AppHost

```csharp
var username = builder.AddParameter("mongo-username", "root");
var password = builder.AddParameter("mongo-password", secret: true);

var mongo = builder.AddMongoDbAtlasLocal("mongodb-atlas-local",
        port: 27027, userName: username, password: password)
    .WithAtlasDataVolume("mongo-atlas-local-data")
    .WithLifetime(ContainerLifetime.Persistent);

var appDb = mongo.AddDatabase("AppMongoDb", "my-app-db");
var analyticsDb = mongo.AddDatabase("AnalyticsMongoDb", "analytics-db");
```

## Connecting Backend Projects

### MongoPlan — Local vs Online Switching

```csharp
internal readonly record struct MongoPlan(bool UseOnline, string Database)
{
    public static MongoPlan Local(string database) => new(false, database);
    public static MongoPlan Online(string database) => new(true, database);
}
```

### WithMongo Extension

```csharp
internal static class MongoExtensions
{
    public static IResourceBuilder<ProjectResource> WithMongo(
        this IResourceBuilder<ProjectResource> backend,
        AppInfrastructure infra,
        MongoPlan plan,
        IResourceBuilder<MongoDbAtlasLocalDatabaseResource> localConnection,
        string connectionKey = "ConnectionStrings:MongoDb",
        string databaseKey = "MongoDb:Database")
    {
        if (plan.UseOnline)
        {
            return backend
                .WithEnvironment(connectionKey, infra.OnlineMongoConnectionString)
                .WithEnvironment(databaseKey, plan.Database);
        }

        return backend
            .WithReference(localConnection)
            .WaitFor(localConnection)
            .WithEnvironment(connectionKey, localConnection)
            .WithEnvironment(databaseKey, plan.Database);
    }
}
```

Pattern:
- Each backend can independently choose local (Atlas Local) or online (production).
- `WithReference` + `WaitFor` ensures backend waits for container readiness.
- Connection key and database key are customizable per backend's configuration structure.

## Cheat Sheet

| Topic | Pattern |
|---|---|
| **Add container** | `builder.AddMongoDbAtlasLocal("name", port: 27027)` |
| **Add database** | `mongo.AddDatabase("DbName", "database-name")` |
| **Data volume** | `.WithAtlasDataVolume("volume-name")` |
| **Search volume** | `.WithSearchVolume("volume-name")` |
| **Persistent** | `.WithLifetime(ContainerLifetime.Persistent)` |
| **Connection** | `mongodb://{user}:{pass}@{host}:{port}` |
| **DB connection** | `{parent}/{dbName}?directConnection=true&authSource=admin` |
| **Stable hostname** | Pass `hostname` param or set `MONGO_ATLAS_HOSTNAME` config |
| **Backend ref** | `.WithReference(db).WaitFor(db).WithEnvironment(key, db)` |
| **Local/Online switch** | `MongoPlan.Local("db")` / `MongoPlan.Online("db")` |

## Best Practices

1. **Pin the image tag** — always `.WithImageTag("8.2.4")`, never use `latest`.
2. **Use `ContainerLifetime.Persistent`** for dev — Atlas Local is slow to start; persistent avoids restarts.
3. **Set hostname for stable replica set** — without it, container restart changes the hostname, breaking connections.
4. **Use `directConnection=true`** for database resources — single-node replica set; avoids topology discovery delays.
5. **Password follows Aspire convention** — auto-generated via `CreateDefaultPasswordParameter` if not passed explicitly.
6. **Mount data + configdb volumes** — `WithAtlasDataVolume` mounts both `/data/db` and `/data/configdb`.
7. **Mount search volume separately** — `WithSearchVolume` mounts `/data/mongot` for Atlas Search index persistence.
8. **Env var difference from standard mongo** — `MONGODB_INITDB_ROOT_*` (with `DB`), not `MONGO_INITDB_ROOT_*`.

## Additional Resources

### Reference Files

- **`references/reviewer-checklist.md`** — Reviewer checklist for Aspire Atlas Local resource definitions
