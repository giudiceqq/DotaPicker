using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using webhook_gateway.DTO;
using webhook_gateway.Interfaces;

namespace webhook_gateway.Singleton;

public class InMemoryDotaMetaCache : IMetaCache
{
    public DateTime? LastUpdateAt { get; set; }
    public bool IsReady { get; set ; } = false;

    public StratzMetaResponse? RawJson { get; set; } = null;
    
    public Dictionary<int, string> HeroNames { get; set; } = new();

    public Dictionary<int, (int winCount, int mathCount)> HeroWinRates { get; set; } = new();

    public Dictionary<(int HeroId, int AllyId), (double Synergy, double WinsAverage)> HeroTeamMatchUps { get; set; } = new();

    public Dictionary<(int HeroId, int EnemyId), double> HeroVsMatchUps { get; set; } = new();

    public Dictionary<int, HeroFullProfile> HeroRoles { get; set; } = new();


    public Task UpdateCacheAsync(StratzMetaResponse data)
    {
        RawJson = data;

        HeroNames = data.constants.heroes.ToDictionary(x => x.id, x => x.displayName);
        HeroWinRates = data.heroStats.winWeek.GroupBy(x => x.heroId)
            .ToDictionary(group => group.Key, group =>
            {
                int totalWins = group.Sum(x => x.winCount);
                int totalMatches = group.Sum(x => x.matchCount);
                return (winCount: totalWins, mathCount: totalMatches);
            });

        HeroTeamMatchUps = data.heroStats.matchUp.SelectMany(x => x.with.Select(ally => new
        {
            Key = (HeroId: x.heroId, AllyId: ally.heroId2),
            Value = (Synergy: ally.synergy, WinsAverage: ally.winsAverage)
        })).ToDictionary(x => x.Key, x => x.Value);

        HeroVsMatchUps = data.heroStats.matchUp.SelectMany(x => x.vs.Select(vs => new
        {
            Key = (HeroId: x.heroId, EnemyId: vs.heroId2),
            Value = vs.winsAverage

        })).ToDictionary(x => x.Key, x => x.Value);


     


    IsReady = true;
        LastUpdateAt = DateTime.UtcNow;
        return Task.CompletedTask;
    }

    public Task UpdateRolesCacheAsync(CurrentPositionOfHeroesMetaResponse data)
    {


        foreach (var element in data.result.CarryPicks)
        {
            decimal percent = Math.Round((decimal)element.Value.matches_s / data.result.TotalMatches[element.Key.ToString()] * 100,2);
            
            
            if (HeroRoles.TryGetValue(element.Key, out var heroFullProfile))
            {
               
                heroFullProfile.CarryMatchesPercent = percent;
            }
            else
            { 
                HeroRoles.Add(element.Key, new HeroFullProfile{CarryMatchesPercent = percent});
            }
          
        }
        
        foreach (var element in data.result.HardSupport)
        {
            decimal percent = Math.Round((decimal)element.Value.matches_s / data.result.TotalMatches[element.Key.ToString()] * 100,2);
            
            
            if (HeroRoles.TryGetValue(element.Key, out var heroFullProfile))
            {
               
                heroFullProfile.HardSupportMatchesPercent = percent;
            }
            else
            { 
                HeroRoles.Add(element.Key, new HeroFullProfile{HardSupportMatchesPercent = percent});
            }
          
        }
        
        foreach (var element in data.result.SoftSupport)
        {
            decimal percent = Math.Round((decimal)element.Value.matches_s / data.result.TotalMatches[element.Key.ToString()] * 100,2);
            
            
            if (HeroRoles.TryGetValue(element.Key, out var heroFullProfile))
            {
               
                heroFullProfile.SoftSupportMatchesPercent = percent;
            }
            else
            { 
                HeroRoles.Add(element.Key, new HeroFullProfile{SoftSupportMatchesPercent = percent});
            }
          
        }
        
        foreach (var element in data.result.OfflanePicks)
        {
            decimal percent = Math.Round((decimal)element.Value.matches_s / data.result.TotalMatches[element.Key.ToString()] * 100,2);
            
            
            if (HeroRoles.TryGetValue(element.Key, out var heroFullProfile))
            {
               
                heroFullProfile.OfflaneMatchesPercent = percent;
            }
            else
            { 
                HeroRoles.Add(element.Key, new HeroFullProfile{OfflaneMatchesPercent = percent});
            }
          
        }
        
        foreach (var element in data.result.MidPicks)
        {
            decimal percent = Math.Round((decimal)element.Value.matches_s / data.result.TotalMatches[element.Key.ToString()] * 100,2);
            
            
            if (HeroRoles.TryGetValue(element.Key, out var heroFullProfile))
            {
               
                heroFullProfile.MidMatchesPercent = percent;
            }
            else
            { 
                HeroRoles.Add(element.Key, new HeroFullProfile{MidMatchesPercent = percent});
            }
          
        }
        
        
        return Task.CompletedTask;
        
    }
    
}

public record HeroFullProfile
{
     public decimal HardSupportMatchesPercent { get; set; }
     public decimal SoftSupportMatchesPercent { get; set; }
     public decimal OfflaneMatchesPercent { get; set; }
     public decimal MidMatchesPercent { get; set; }
     public decimal CarryMatchesPercent { get; set; }
     
}