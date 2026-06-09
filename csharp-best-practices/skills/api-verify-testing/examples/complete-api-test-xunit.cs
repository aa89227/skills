// =============================================================================
// Example: Complete API integration test with Verify snapshots (xUnit v3)
// Shows how all components work together: Extension + TestHelper + Test class
// =============================================================================

// --- 1. Typed HttpClient Extension (Common/Extensions/OrderHttpClientExtension.cs) ---
// (Identical to NUnit — framework-agnostic, see complete-api-test.cs)

// --- 2. Collection + ApiTestBase (Common/ApiTestBase.cs) ---

using Microsoft.AspNetCore.Mvc.Testing;

[CollectionDefinition]
public class ApiCollection : ICollectionFixture<TestServer>;

[Collection<ApiCollection>]
public abstract class ApiTestBase(TestServer server) : IAsyncLifetime
{
    internal TestServer Server { get; } = server;
    protected HttpClient Client => Server.CreateClient();

    public async ValueTask InitializeAsync()
    {
        await Server.DropDatabaseAsync();
        Server.ResetIdGenerator();
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}

// --- 3. TestHelper extension (Common/Utils.cs) ---
// (Identical to NUnit — framework-agnostic, see complete-api-test.cs)

// --- 4. Test class (OrderApiVerifyTest.cs) ---

using VerifyTests;

public sealed class OrderApiVerifyTest(TestServer server) : ApiTestBase(server)
{
    [Fact]
    public async Task QueryOrdersByCustomerId()
    {
        // Arrange
        await Server.CreateOrder(customerId: "customer-1", productName: "Product A");
        await Server.CreateOrder(customerId: "customer-1", productName: "Product B");

        // Act
        var response = await Client.Order().GetOrdersByCustomerAsync("customer-1");

        // Assert
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync();
        await VerifyJsonSnapshotAsync(json, "Order.QueryByCustomer");
    }

    [Fact]
    public async Task UpdateOrderStatusThenQuery()
    {
        // Arrange
        var orderId = await Server.CreateOrder();

        // Act
        var updateResponse = await Client.Order().UpdateOrderStatusAsync(
            orderId, new OrderHttpClient.UpdateOrderStatusRequest("shipped"));

        // Assert
        updateResponse.EnsureSuccessStatusCode();

        // Act
        var queryResponse = await Client.Order().GetOrderAsync(orderId);

        // Assert
        queryResponse.EnsureSuccessStatusCode();
        var json = await queryResponse.Content.ReadAsStringAsync();
        await VerifyJsonSnapshotAsync(json, "Order.StatusUpdatedToShipped");
    }

    private async Task VerifyJsonSnapshotAsync(string json, string snapshotName)
    {
        var settings = new VerifySettings();
        settings.UseFileName(snapshotName);
        settings.UseStrictJson();
        await VerifyJson(json, settings);
    }
}
