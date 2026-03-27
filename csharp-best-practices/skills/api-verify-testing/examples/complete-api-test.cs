// =============================================================================
// Example: Complete API integration test with Verify snapshots
// Shows how all components work together: Extension + TestHelper + Test class
// =============================================================================

// --- 1. Typed HttpClient Extension (Common/Extensions/OrderHttpClientExtension.cs) ---

using System.Net.Http.Json;

internal static class OrderHttpClientExtension
{
    internal static OrderHttpClient Order(this HttpClient client)
        => new(client);
}

internal class OrderHttpClient(HttpClient client)
{
    private const string BaseUrl = "/orders";

    public Task<HttpResponseMessage> CreateOrderAsync(CreateOrderRequest request)
        => client.PostAsJsonAsync(BaseUrl, request);

    public Task<HttpResponseMessage> GetOrderAsync(string orderId)
        => client.GetAsync($"{BaseUrl}/{orderId}");

    public Task<HttpResponseMessage> GetOrdersByCustomerAsync(string customerId)
        => client.GetAsync($"{BaseUrl}?customerId={customerId}");

    public Task<HttpResponseMessage> UpdateOrderStatusAsync(string orderId, UpdateOrderStatusRequest request)
        => client.PostAsJsonAsync($"{BaseUrl}/{orderId}/status", request);

    public Task<HttpResponseMessage> DeleteOrderAsync(string orderId)
        => client.DeleteAsync($"{BaseUrl}/{orderId}");

    public sealed record CreateOrderRequest(string CustomerId, string ProductName, int Quantity);
    public sealed record UpdateOrderStatusRequest(string Status);
}

// --- 2. TestHelper extension (Common/Utils.cs) ---

internal static class TestHelper
{
    public static async Task<string> CreateOrder(this TestServer server,
        string customerId = "customer-1", string productName = "Product A", int quantity = 1)
    {
        var client = server.CreateClient();
        var response = await client.Order().CreateOrderAsync(
            new OrderHttpClient.CreateOrderRequest(customerId, productName, quantity));
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<string>())!;
    }
}

// --- 3. Test class (OrderApiVerifyTest.cs) ---

using VerifyNUnit;
using VerifyTests;

public sealed class OrderApiVerifyTest : ApiTestBase
{
    [Test]
    [Description("""
    Given: A customer has placed two orders
    When: Query orders by customer ID
    Then: Should return both orders sorted by creation time
    """)]
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

    [Test]
    [Description("""
    Given: An order exists with status "pending"
    When: Update the order status to "shipped"
    Then: Should return success
    When: Query the order
    Then: Should show status as "shipped"
    """)]
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
