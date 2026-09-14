using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using webhook_gateway.DB;
using webhook_gateway.DTO;
using webhook_gateway.Interfaces;
using webhook_gateway.Models;

namespace webhook_gateway.Services;

public class BackgroundDataFetcher(IServiceScopeFactory serviceScopeFactory, IMetaCache metaCache, IMetaDataFetcher metaDataFetcher) : BackgroundService
{
    
    private DateTime? LastTimeUpdated { get; set; } = null;
    


    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            using var scope = serviceScopeFactory.CreateScope();
            var dataBase = scope.ServiceProvider.GetService<AppDbContext>();
            var lastDotaMetaSnapshot = await dataBase.DotaMetaSnapshots.OrderByDescending(x => x.CreatedAt).FirstOrDefaultAsync(stoppingToken);
            var lastHeroRolesStatistic = await dataBase.CurrentPositions.OrderByDescending(x => x.CreatedAt).FirstOrDefaultAsync(stoppingToken);
            if (lastHeroRolesStatistic == null) throw new NullReferenceException("No last hero roles snapshot found");
            if(lastDotaMetaSnapshot == null) throw new NullReferenceException("No last payload found");
            
            await metaCache.UpdateCacheAsync(lastDotaMetaSnapshot.Payload);
            await metaCache.UpdateRolesCacheAsync(lastHeroRolesStatistic.Payload.data);
            
            
            LastTimeUpdated = lastDotaMetaSnapshot.CreatedAt;
            Console.WriteLine($"Current payload was found! {LastTimeUpdated}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Unable to read DB {ex.Message}");
        }
        
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                if (!LastTimeUpdated.HasValue)
                {
                    using var serviceScope = serviceScopeFactory.CreateScope();

                    var dataBase = serviceScope.ServiceProvider.GetRequiredService<AppDbContext>();

                    var fetchedData = await metaDataFetcher.FetchMetaDataAsync(stoppingToken);
            
                    var rolesData = await metaDataFetcher.FetchMetaDataOfHeroRolesAsync(stoppingToken);
                    
                    if (fetchedData == null) throw new Exception("Failed to download meta data");

                    if(rolesData == null) throw new Exception("Failed to download roles meta data");
                    
                    
                    var snapShot = new DotaMetaSnapshot
                    {
                        Payload = fetchedData.data
                    };

                    var rolesSnapshot = new DotaHeroRolesStatistic
                    {
                        Payload = rolesData
                    };

                    dataBase.DotaMetaSnapshots.Add(snapShot);
                    
                    
                    dataBase.CurrentPositions.Add(rolesSnapshot);
                    
                    
                    await metaCache.UpdateCacheAsync(snapShot.Payload);
                    
                    await metaCache.UpdateRolesCacheAsync(rolesData.data);
                    
                    LastTimeUpdated = DateTime.UtcNow;
                    await dataBase.SaveChangesAsync(stoppingToken);

                }
                else
                {
                    var timePassed = DateTime.UtcNow - LastTimeUpdated.Value;
    
                  
                    var timeRemaining = TimeSpan.FromDays(7) - timePassed;
                    if (timeRemaining <= TimeSpan.Zero)
                    {
                        LastTimeUpdated = null;
                        continue;
                    }
                    
                    Console.WriteLine($"Another try in {timeRemaining.TotalHours:F2} hours");
                    await Task.Delay(timeRemaining, stoppingToken);

                }
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine($"Stratz API is unavailable {ex.StatusCode}:{ex.Message}");
                await Task.Delay(TimeSpan.FromMinutes(2), stoppingToken);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Critical Error while fetching meta data: {ex.Message}");
                await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
            }
        }
    }
}