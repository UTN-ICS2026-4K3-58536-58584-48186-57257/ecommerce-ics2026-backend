namespace Dsw2025Tpi.Domain.Entities;

public class Customer : EntityBase
{
    public string EMail { get; set; }
    public string Name { get; set; }
    public string PhoneNumber { get; set; }
    public string? IdentityUserId { get; set; }
    public ICollection<Order> Orders { get; set; }
}
