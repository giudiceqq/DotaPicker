using webhook_gateway.DTO;

namespace webhook_gateway.Models;

public class DotaMetaSnapshot
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public StratzMetaResponse Payload { get; set; }

    
}