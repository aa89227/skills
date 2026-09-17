# Standalone Playwright E2E with Testcontainers

Load this reference when the application under test must run without an Aspire AppHost. The
recommended topology starts the application and its dependencies explicitly, keeps the browser in
a container, and gives each parallel worker isolated mutable state.

## When to use this mode

Use standalone hosting by default for a normal ASP.NET Core or Blazor E2E suite. Select Aspire only
when the user explicitly requests AppHost orchestration or the application requirement genuinely
depends on Aspire resource wiring.

Standalone hosting is a better fit when the suite needs:

- direct control over application startup, ports, configuration, and shutdown;
- one long-lived database container with separate databases or schemas for parallel workers;
- independent external-service stubs per worker;
- a browser that exercises the application's real HTTP and WebSocket endpoints; or
- predictable startup that does not depend on DCP resource-state transitions.

This mode does not mean that the application must be started as a separately maintained deployment.
For an ASP.NET Core application, `WebApplicationFactory<Program>` with real Kestrel is sufficient.
For separate frontend and backend projects, start each process explicitly and give each a bounded
health check and an explicit endpoint.

## Topology

```text
xUnit assembly fixture
  ├─ shared PostgreSQL container
  ├─ shared Playwright browser container
  └─ one E2E worker per parallel test class
       ├─ isolated database or schema
       ├─ WebApplicationFactory<Program> + Kestrel on a dynamic port
       └─ WireMock servers for external HTTP dependencies
```

Share expensive containers, not mutable test state. A worker owns its application host, database
or schema, and external-service stubs. A fresh browser context belongs to each test method.

## Application host

The default `WebApplicationFactory` transport uses an in-memory `TestServer`. A browser in a
separate container cannot reach that transport. Configure the factory to use real Kestrel on a
dynamic port, and inject test configuration before the application builds:

```csharp
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

internal sealed class E2EWebApplicationFactory(
    IReadOnlyDictionary<string, string?> configuration)
    : WebApplicationFactory<Program>
{
    protected override IHost CreateHost(IHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.ConfigureHostConfiguration(config =>
            config.AddInMemoryCollection(configuration));

        return base.CreateHost(builder);
    }
}
```

Create one factory per worker and configure it before starting the server:

```csharp
var factory = new E2EWebApplicationFactory(new Dictionary<string, string?>
{
    ["ConnectionStrings:Example"] = workerConnectionString,
    ["ExternalServices:ExampleEndpoint"] = wireMockUrl,
});

factory.UseKestrel(0);
factory.StartServer();

var localAddress = factory.ClientOptions.BaseAddress
    ?? throw new InvalidOperationException("The test host did not expose a Kestrel address.");
```

Rules:

- Use host configuration (`ConfigureHostConfiguration`) when the application's entry point reads
  settings before `Build`; late `ConfigureAppConfiguration` changes may be too late.
- Use a real health endpoint such as `/alive` and poll it with a bounded timeout after Kestrel
  starts. Do not treat construction of the factory as proof that the application is ready.
- Keep the dynamic port and worker endpoint in fixture state. Do not hard-code a port that can collide
  with another test process.
- If frontend and backend are separate, start both explicitly, wait for both health endpoints, and
  pass the backend URL through the frontend's test configuration. Do not add an AppHost solely to
  launch the two processes.

## Database container and worker isolation

Start one database container at assembly scope and create one database or schema per worker. The
source project uses a PostgreSQL container, an administrative connection for database creation, and
`Database.MigrateAsync()` before starting each application host:

```csharp
using Npgsql;
using Testcontainers.PostgreSql;

var postgres = new PostgreSqlBuilder("postgres:<resolved-image-tag>")
    .WithDatabase("e2e_admin")
    .WithUsername("postgres")
    .WithPassword("postgres")
    .Build();

await postgres.StartAsync();
var adminConnectionString = postgres.GetConnectionString();

// Allocate a unique name per worker, create it through the admin connection,
// and build the worker connection string with NpgsqlConnectionStringBuilder.
var workerConnectionString = CreateWorkerDatabase(adminConnectionString, workerDatabaseName);

await using var database = CreateDbContext(workerConnectionString);
await database.Database.MigrateAsync();
```

Use a safe identifier-quoting helper when creating a database from a generated name. Never
interpolate user-controlled input into the SQL identifier. If the provider supports schema
isolation and that is sufficient for the application, a schema per worker is also valid.

The worker boundary should look like this:

```text
E2EWorker
  ├─ worker database/schema
  ├─ E2EWebApplicationFactory + Kestrel
  └─ WireMock instances
```

Before each test:

1. Clear mutable tables or reset the worker schema.
2. Reset external-service stubs to their defaults.
3. Seed the exact data needed by the test through the application database model or a test helper.
4. Create a new browser context.

Preserve users and data-protection keys when tests reuse authentication cookies. A test that must
seed data before creating its context should call the reset helper first, seed the data, and opt out
of the context helper's second reset.

## xUnit fixture lifetime and parallelism

Use an xUnit v3 assembly fixture for the shared PostgreSQL and browser containers:

```csharp
[assembly: AssemblyFixture(typeof(E2EAssemblyFixture))]

public sealed class E2EAssemblyFixture : IAsyncLifetime
{
    public ValueTask InitializeAsync() => ValueTask.CompletedTask;

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}
```

The real fixture starts the shared containers, creates all worker databases, migrates them, starts
each worker host, and only then exposes the worker ports to the browser container. A class fixture
selects one worker:

```csharp
public sealed class FeatureE2ETests(E2EAppFixture fixture)
    : IClassFixture<E2EAppFixture>
{
    [Fact]
    public async Task ShouldShowTheExpectedResult()
    {
        await using var context = await fixture.NewContextAsync();
        var page = await context.NewPageAsync();

        await page.GotoAsync("/feature");
        await Expect(page.GetByRole(AriaRole.Heading)).ToBeVisibleAsync();
    }
}
```

xUnit creates class fixtures lazily. If every test class needs a preallocated worker, build the worker
pool during assembly-fixture initialization or use an explicit non-parallel collection policy. Do not
allocate a new database, server, or port during the first test method in an otherwise parallel suite.

Do not use `ICollectionFixture` as a default merely because an Aspire example used it. Use a
collection fixture only when the standalone tests intentionally share the same mutable boundary.

## Browser container and host connectivity

Run the browser in the official Playwright container. Resolve the latest stable `Microsoft.Playwright`
package version at task time and use that exact resolved version for the container image and the
`npx playwright` client; never copy a version from this reference.

Expose every worker's Kestrel port before creating the browser container:

```csharp
var workerPorts = workers.Select(static worker => worker.Port).ToArray();
await TestcontainersSettings.ExposeHostPortsAsync(workerPorts);

var browserContainer = new ContainerBuilder(
        "mcr.microsoft.com/playwright:v<resolved-Microsoft.Playwright-version>-noble")
    .WithEntrypoint("/bin/sh", "-c")
    .WithCommand(
        "npx -y playwright@<resolved-Microsoft.Playwright-version> " +
        "run-server --port 8080 --host 0.0.0.0")
    .WithPortBinding(8080, assignRandomHostPort: true)
    .WithWaitStrategy(Wait.ForUnixContainer().UntilMessageIsLogged("Listening on"))
    .Build();

await browserContainer.StartAsync();
var playwright = await Playwright.CreateAsync();
var browser = await playwright.Chromium.ConnectAsync(
    $"ws://{browserContainer.Hostname}:{browserContainer.GetMappedPublicPort(8080)}/");
```

`TestcontainersSettings.ExposeHostPortsAsync` is process-wide in common Testcontainers versions.
Serialize initialization with a lock and expose all worker ports in one call before the browser
container starts. From the browser container, construct the application URL with:

```csharp
var browserBaseUrl = new UriBuilder(localAddress)
{
    Host = "host.testcontainers.internal",
}.Uri.AbsoluteUri;
```

Use `host.testcontainers.internal`, not Docker-only `host.docker.internal`; the former works across
Docker and Podman with Testcontainers' host-port forwarding.

## Browser context and authentication

Create a new context for every test, with deterministic rendering options and an optional cookie
obtained through the application's real authentication path:

```csharp
public async Task<IBrowserContext> NewContextAsync(
    Cookie? authCookie = null,
    ViewportSize? viewport = null)
{
    await ResetTestDataAsync();

    var context = await Browser.NewContextAsync(new BrowserNewContextOptions
    {
        BaseURL = BaseUrl,
        ViewportSize = viewport ?? new ViewportSize { Width = 1280, Height = 720 },
        ReducedMotion = ReducedMotion.Reduce,
        ColorScheme = ColorScheme.Light,
        DeviceScaleFactor = 1,
    });

    if (authCookie is not null)
    {
        await context.AddCookiesAsync([authCookie]);
    }

    return context;
}
```

For authenticated scenarios, use a secured test-only login/bootstrap endpoint or the same sign-in
flow used by the application to obtain a real cookie. Do not forge a cookie value or bypass the
application's authentication middleware. Create commonly used roles/users once per worker, then
copy only the cookie into each test context.

## Readiness for interactive applications

`DOMContentLoaded`, `Load`, and `NetworkIdle` are useful signals but are not sufficient for an
interactive server-rendered application. Use the application's own readiness contract:

- poll the health endpoint after the host starts;
- wait for the first application WebSocket/circuit frame when the UI replaces prerendered markup;
- wait for a stable, feature-specific DOM condition before interacting; and
- before screenshots, await `document.fonts.ready`, wait for images, and allow rendering to settle.

Use bounded waits and diagnostic evidence. Avoid unbounded polling and arbitrary long sleeps.

## WireMock and external services

Start one WireMock server per worker. Inject its URL into the application before host creation, so
the production HTTP client and service path remain real while the remote dependency is deterministic:

```csharp
var wireMock = WireMockServer.Start();
var configuration = new Dictionary<string, string?>
{
    ["ExternalServices:ExampleEndpoint"] = $"http://127.0.0.1:{wireMock.Port}/api",
};

var factory = new E2EWebApplicationFactory(configuration);
factory.UseKestrel(0);

wireMock
    .Given(Request.Create().WithPath("/api/result").UsingPost())
    .RespondWith(Response.Create()
        .WithStatusCode(200)
        .WithHeader("Content-Type", "application/json")
        .WithBody("{\"status\":\"ok\"}"));
```

Reset the server and reapply default stubs in the per-test reset helper. Keep test-specific stubs
local to the test that needs them. Do not point tests at a developer's live external service.

## Visual regression artifacts

Use the existing [visual-regression.md](visual-regression.md) guidance for stable screenshots. A
standalone fixture should expose helpers that:

- derive class and method names from the current test context;
- store baselines under a deterministic source-controlled directory;
- save a received image beside the baseline on mismatch;
- disable animations and hide the caret;
- mask dynamic navigation, timestamps, IDs, or other unstable content; and
- support both full-page and element screenshots.

Do not silently overwrite a baseline after a mismatch. Treat baseline updates as an explicit review
decision.

## Disposal order

Dispose resources in the reverse dependency order:

1. test browser contexts and pages;
2. worker application hosts and WireMock servers;
3. the shared Playwright browser and its container; and
4. the shared database container.

Always dispose the browser context with `await using`. Keep the raw failure screenshot, video, and
runner log paths in the test report so a failed test is diagnosable without rerunning it.

## Rules

1. Standalone host plus Testcontainers is the default; Aspire is opt-in.
2. Use real Kestrel for a browser running outside the application process; do not expose an in-memory
   `TestServer` as if it were a network endpoint.
3. Share expensive containers, but isolate mutable state by worker and reset it before each test.
4. Expose all host ports before creating the browser container and use
   `host.testcontainers.internal` for container-to-host URLs.
5. Inject configuration before application build and use bounded health/readiness checks.
6. Resolve package and browser image versions online at task time; keep Playwright versions aligned.
7. Keep screenshot, video, and runner artifacts deterministic and out of accidental source commits.
