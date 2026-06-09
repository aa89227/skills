// =============================================================================
// Example: Testing API that calls external SMS and Payment services
// Shows FakeHttpHandler setup, response queuing, request body/header assertion
// =============================================================================

// --- 1. FakeHttpHandler (Common/Fakes/FakeHttpHandler.cs) ---

using System.Net;
using System.Net.Http.Json;

internal sealed class FakeHttpHandler : DelegatingHandler
{
    public List<CapturedRequest> Requests { get; } = [];
    private readonly Queue<HttpResponseMessage> _responses = new();

    public void EnqueueResponse(HttpStatusCode status = HttpStatusCode.OK)
        => _responses.Enqueue(new HttpResponseMessage(status));

    public void EnqueueJsonResponse<T>(T body, HttpStatusCode status = HttpStatusCode.OK)
        => _responses.Enqueue(new HttpResponseMessage(status) { Content = JsonContent.Create(body) });

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken ct)
    {
        var body = request.Content is not null
            ? await request.Content.ReadAsStringAsync(ct)
            : null;

        Requests.Add(new CapturedRequest(
            request.Method,
            request.RequestUri!,
            body,
            request.Headers.ToDictionary(h => h.Key, h => string.Join(", ", h.Value))));

        return _responses.TryDequeue(out var response)
            ? response
            : new HttpResponseMessage(HttpStatusCode.OK);
    }

    public void Reset()
    {
        Requests.Clear();
        _responses.Clear();
    }

    public sealed record CapturedRequest(
        HttpMethod Method, Uri RequestUri, string? Body, Dictionary<string, string> Headers);
}

// --- 2. TestServer registration (TestServer.cs — additions) ---

using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

internal class TestServer(InitialParameter param) : WebApplicationFactory<Program>
{
    internal FakeHttpHandler SmsHandler { get; } = new();
    internal FakeHttpHandler PaymentHandler { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureTestServices(services =>
        {
            // ... existing config (connection strings, FakeIdGenerator, etc.)

            services.AddHttpClient("SmsService")
                .AddHttpMessageHandler(() => SmsHandler);
            services.AddHttpClient("PaymentService")
                .AddHttpMessageHandler(() => PaymentHandler);
        });
    }

    public void ResetFakeHandlers()
    {
        SmsHandler.Reset();
        PaymentHandler.Reset();
    }
}

// --- 3. Test class (OrderNotificationTest.cs) ---

using VerifyNUnit;
using VerifyTests;

public sealed class OrderNotificationTest : ApiTestBase
{
    [Test]
    [Description("""
    Given: A completed order exists
    When: Trigger order shipment notification
    Then: Should send SMS with correct order details
    """)]
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

        Assert.That(Server.SmsHandler.Requests, Has.Count.EqualTo(1));
        var captured = Server.SmsHandler.Requests[0];
        await VerifyJsonSnapshotAsync(captured.Body!, "Order.ShipmentSmsRequest");
    }

    [Test]
    [Description("""
    Given: A completed order exists
    When: Trigger payment refund
    Then: Should send refund request with correct API key header
    """)]
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
        Assert.That(captured.Headers, Does.ContainKey("X-Api-Key"));
        Assert.That(captured.Method, Is.EqualTo(HttpMethod.Post));
    }

    [Test]
    [Description("""
    Given: External SMS service returns 503
    When: Trigger order notification
    Then: API should return 200 (fire-and-forget) but log the failure
    """)]
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

    [Test]
    [Description("""
    Given: An order cancelled without shipment
    When: Cancel the order
    Then: No SMS should be sent
    """)]
    public async Task CancelledOrderDoesNotSendSms()
    {
        // Arrange
        var orderId = await Server.CreateOrder();

        // Act
        var response = await Client.Order().UpdateOrderStatusAsync(
            orderId, new OrderHttpClient.UpdateOrderStatusRequest("cancelled"));

        // Assert
        response.EnsureSuccessStatusCode();
        Assert.That(Server.SmsHandler.Requests, Is.Empty);
    }

    private async Task VerifyJsonSnapshotAsync(string json, string snapshotName)
    {
        var settings = new VerifySettings();
        settings.UseFileName(snapshotName);
        settings.UseStrictJson();
        await VerifyJson(json, settings);
    }
}
