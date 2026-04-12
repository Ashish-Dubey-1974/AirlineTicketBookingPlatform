using System.Text.Json.Serialization;

namespace SkyBooker.Payment.API.DTOs;

public class RazorpayWebhookPayload
{
    [JsonPropertyName("event")]
    public string Event { get; set; } = string.Empty;

    [JsonPropertyName("payload")]
    public Payload Payload { get; set; } = new();
}

public class Payload
{
    [JsonPropertyName("payment")]
    public PaymentDetails Payment { get; set; } = new();

    [JsonPropertyName("order")]
    public OrderDetails Order { get; set; } = new();
}

public class PaymentDetails
{
    [JsonPropertyName("entity")]
    public PaymentEntity Entity { get; set; } = new();
}

public class PaymentEntity
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("order_id")]
    public string OrderId { get; set; } = string.Empty;

    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    [JsonPropertyName("amount")]
    public decimal Amount { get; set; }

    [JsonPropertyName("currency")]
    public string Currency { get; set; } = string.Empty;

    [JsonPropertyName("method")]
    public string Method { get; set; } = string.Empty;
}

public class OrderDetails
{
    [JsonPropertyName("entity")]
    public OrderEntity Entity { get; set; } = new();
}

public class OrderEntity
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("receipt")]
    public string Receipt { get; set; } = string.Empty;
}

public class StripeWebhookPayload
{
    public string Id { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public StripeData Data { get; set; } = new();
}

public class StripeData
{
    public StripePaymentIntent Object { get; set; } = new();
}

public class StripePaymentIntent
{
    public string Id { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public long Amount { get; set; }
    public string Currency { get; set; } = string.Empty;
    public string PaymentMethod { get; set; } = string.Empty;
    public string LatestCharge { get; set; } = string.Empty;
}
