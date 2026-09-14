namespace webhook_gateway.DTO;

public class DotaHeroRolesStatistic
{
    public Guid Id { get; set; } = Guid.NewGuid();
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public CurrentPositionData Payload { get; set; }
    
}