using pcheli_diplomenproekt.Dtos;
using pcheli_diplomenproekt.Models;
using pcheli_diplomenproekt.Services;

namespace pcheli_diplomenproekt.Tests;

/// <summary>Записва кои известия са „изпратени“, без да праща писма.</summary>
public sealed class FakeNotifier : INotificationService
{
    public List<string> Created { get; } = new();
    public List<(string Number, string Status)> StatusChanges { get; } = new();
    public List<string> Paid { get; } = new();
    public List<(string Apiary, bool Approved)> Decisions { get; } = new();

    public void OrderCreated(Order order) => Created.Add(order.OrderNumber);
    public void OrderStatusChanged(Order order) => StatusChanges.Add((order.OrderNumber, order.Status));
    public void OrderPaid(Order order) => Paid.Add(order.OrderNumber);
    public void ApplicationDecided(BeekeeperApplication application, bool approved) => Decisions.Add((application.ApiaryName, approved));

    public List<(string Email, string Link)> PasswordResets { get; } = new();
    public List<(string Email, string Link)> Confirmations { get; } = new();
    public List<(string Subject, string? Shop, string? Beekeeper)> Contacts { get; } = new();
    public List<(string Email, string Apiary, string OrderNumber)> BeekeeperOrders { get; } = new();
    public List<(string Email, string Product)> StockAvailables { get; } = new();
    public List<(string Email, string Subject)> Newsletters { get; } = new();

    public void PasswordReset(string email, string name, string link) => PasswordResets.Add((email, link));
    public void EmailConfirmation(string email, string name, string link) => Confirmations.Add((email, link));
    public void ContactReceived(ContactMessage message, string? shopEmail, string? beekeeperEmail) => Contacts.Add((message.Subject, shopEmail, beekeeperEmail));
    public void BeekeeperNewOrder(string email, string apiaryName, Order order, IReadOnlyCollection<OrderItem> items) => BeekeeperOrders.Add((email, apiaryName, order.OrderNumber));
    public void StockAvailable(string email, string productName, string url) => StockAvailables.Add((email, productName));
    public void Newsletter(string email, string subject, string htmlBody, string unsubscribeUrl) => Newsletters.Add((email, subject));
}

public static class Make
{
    public static OrderService Orders(TestDb db, FakeNotifier? notifier = null, IPaymentService? payments = null) =>
        new(db.Context, new PromoService(db.Context), new ShippingService(), notifier, payments);

    public static CreateOrderDto Order(int variantId, int qty = 1, string? promo = null) => new()
    {
        CustomerName = "Клиент", CustomerPhone = "0888 111 222", CustomerEmail = "klient@example.com",
        DeliveryCity = "София", DeliveryAddress = "ул. Тест 1", PromoCode = promo,
        Items = { new CreateOrderItemDto { ProductVariantId = variantId, Quantity = qty } }
    };

    public static ApplicationUser User(TestDb db, string email = "user@example.com")
    {
        var u = new ApplicationUser { UserName = email, NormalizedUserName = email.ToUpperInvariant(), Email = email, FullName = "Тест Потребител" };
        db.Context.Users.Add(u);
        db.Context.SaveChanges();
        return u;
    }
}
