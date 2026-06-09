# Aspire Atlas Local — Reviewer Checklist

When reviewing Aspire AppHost code with MongoDB Atlas Local resources, you **must** use the Todo tool or create a checklist file to track each item and ensure every check is completed.

## Checklist

### Resource Definition
- [ ] `MongoDbAtlasLocalResource` extends `ContainerResource, IResourceWithConnectionString`
- [ ] `MongoDbAtlasLocalDatabaseResource` extends `Resource, IResourceWithConnectionString, IResourceWithParent<MongoDbAtlasLocalResource>`
- [ ] Connection string expression uses `ReferenceExpression.Create`
- [ ] Database connection includes `?directConnection=true&authSource=admin`

### Container Configuration
- [ ] Image is `mongodb/mongodb-atlas-local` with pinned tag (e.g., `8.2.6`)
- [ ] Environment variables use `MONGODB_INITDB_ROOT_USERNAME/PASSWORD` (not `MONGO_`)
- [ ] Hostname is set for stable replica set (env var + `--hostname` container arg)
- [ ] Password auto-generated via `CreateDefaultPasswordParameter` when not provided

### Volume Mounts
- [ ] Data volume: `/data/db` and `/data/configdb` via `WithAtlasDataVolume`
- [ ] Search volume: `/data/mongot` via `WithSearchVolume` (if Atlas Search used)

### AppHost Usage
- [ ] Container uses `ContainerLifetime.Persistent` for development
- [ ] Backend projects use `.WithReference(db).WaitFor(db)`
- [ ] Connection key and database key match the backend's configuration expectations
- [ ] `ParameterResource.Value` avoided in runtime callbacks (deprecated in Aspire 13.x)
- [ ] `Aspire.Hosting.JavaScript` used instead of deprecated `Aspire.Hosting.NodeJs`
