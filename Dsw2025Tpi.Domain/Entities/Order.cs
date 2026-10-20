namespace Dsw2025Tpi.Domain.Entities;

public class Order : EntityBase
{
    public Guid CustomerId { get; set; }
    public Customer Customer { get; set; }
    public string ShippingAddress { get; set; }
    public string BillingAddress { get; set; }
    public OrderStatus Status { get; set; } = OrderStatus.PENDING; 
    public DateTime OrderDate { get; set; } = DateTime.UtcNow;
    public decimal TotalAmount { get; set; } 
    public List<OrderItem> OrderItems { get; set; } = new();
}
