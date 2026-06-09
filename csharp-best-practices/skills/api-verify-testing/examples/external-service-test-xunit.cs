// =============================================================================
// Example: Testing API that calls external SMS and Payment services (xUnit v3)
// Shows FakeHttpHandler setup, response queuing, request body/header assertion
// =============================================================================

// --- 1. FakeHttpHandler (Common/Fakes/FakeHttpHandler.cs) ---
// (Identical to NUnit — framework-agnostic, see external-service-test.cs)

// --- 2. TestServer registration (TestServer.cs — additions) ---
// (Identical to NUnit — framework-agnostic, see external-service-test.cs)

// --- 3. ApiTestBase with FakeHandler reset ---

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
        Server.ResetFakeHandlers();
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}

// --- 4. Test class (OrderNotificationTest.cs) ---

using System.Net;
using VerifyTests;

public sealed class OrderNotificationTest(TestServer server) : ApiTestBase(server)
{
    [Fact]
    public async Task ShipmentNotificationSendsCorrectSmsBody()
    {
        // Arrange
        var orderId = await Server.CreateOrder(productName: "Laptop", quantity: 2);
        Server.SmsHandler.EnqueueJsonResponse(new { messageId = "msg-001", status = "sent" });

        // Act
        var response = await Client.Order().UpdateOrderStatusAsync(
            orderId, new OrderHttpClient.UpdateOrderStatusRequest("shipped"));

        // Assert
        response.EnsureSuccessStatusCode();

        Assert.Single(Server.SmsHandler.Requests);
        var captured = Server.SmsHandler.Requests[0];
        await VerifyJsonSnapshotAsync(captured.Body!, "Order.ShipmentSmsRequest");
    }

    [Fact]
    public async Task RefundRequestIncludesApiKeyHeader()
    {
        // Arrange
        var orderId = await Server.CreateOrder();
        Server.PaymentHandler.EnqueueJsonResponse(
            new { refundId = "ref-001", status = "processed" });

        // Act
        var response = await Client.Order().DeleteOrderAsync(orderId);

        // Assert
        response.EnsureSuccessStatusCode();

        var captured = Server.PaymentHandler.Requests[0];
        Assert.Contains("X-Api-Key", captured.Headers);
        Assert.Equal(HttpMethod.Post, captured.Method);
    }

    [Fact]
    public async Task SmsServiceUnavailableDoesNotBlockOrder()
    {
        // Arrange
        var orderId = await Server.CreateOrder();
        Server.SmsHandler.EnqueueResponse(HttpStatusCode.ServiceUnavailable);

        // Act
        var response = await Client.Order().UpdateOrderStatusAsync(
            orderId, new OrderHttpClient.UpdateOrderStatusRequest("shipped"));

        // Assert
        response.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task CancelledOrderDoesNotSendSms()
    {
        // Arrange
        var orderId = await Server.CreateOrder();

        // Act
        var response = await Client.Order().UpdateOrderStatusAsync(
            orderId, new OrderHttpClient.UpdateOrderStatusRequest("cancelled"));

        // Assert
        response.EnsureSuccessStatusCode();
        Assert.Empty(Server.SmsHandler.Requests);
    }

    private async Task VerifyJsonSnapshotAsync(string json, string snapshotName)
    {
        var settings = new VerifySettings();
        settings.UseFileName(snapshotName);
        settings.UseStrictJson();
        await VerifyJson(json, settings);
    }
}
