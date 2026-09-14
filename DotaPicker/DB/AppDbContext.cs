using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using webhook_gateway.DTO;
using webhook_gateway.Models;

namespace webhook_gateway.DB;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
        
    }
    
    public DbSet<DotaMetaSnapshot> DotaMetaSnapshots { get; set; }
    
    public DbSet<DotaHeroRolesStatistic> CurrentPositions { get; set; }
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        modelBuilder.Entity<DotaMetaSnapshot>()
            .Property(x => x.Payload)
            .HasConversion(
                v => JsonSerializer.Serialize(v, (JsonSerializerOptions)null),
                v => JsonSerializer.Deserialize<StratzMetaResponse>(v, (JsonSerializerOptions)null)
            );
        modelBuilder.Entity<DotaHeroRolesStatistic>().Property(x => x.Payload).HasConversion(
            v => JsonSerializer.Serialize(v, (JsonSerializerOptions)null),
            v => JsonSerializer.Deserialize<CurrentPositionData>(v, (JsonSerializerOptions)null));
    }
    
}