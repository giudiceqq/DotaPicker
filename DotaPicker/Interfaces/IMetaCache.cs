using webhook_gateway.DTO;
using webhook_gateway.Singleton;

namespace webhook_gateway.Interfaces;

public interface IMetaCache
{
    public DateTime? LastUpdateAt { get; set; }
    bool IsReady { get; set ; }   
    
    Dictionary<int,string> HeroNames { get; set;  } 
    
    Dictionary<int, (int winCount, int mathCount)> HeroWinRates { get; set; }
    
    Dictionary<(int HeroId, int AllyId), (double Synergy, double WinsAverage)> HeroTeamMatchUps { get; set; }
    
    Dictionary<(int HeroId, int EnemyId), double> HeroVsMatchUps  { get; set; }

    Dictionary<int, HeroFullProfile> HeroRoles { get; set; } 
        
    Task UpdateCacheAsync(StratzMetaResponse data);
    
    Task UpdateRolesCacheAsync(CurrentPositionOfHeroesMetaResponse data);

}