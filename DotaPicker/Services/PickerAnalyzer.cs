using System.Diagnostics;
using System.Net;
using Microsoft.AspNetCore.Identity;
using webhook_gateway.Commands;
using webhook_gateway.Interfaces;

namespace webhook_gateway.Services;

public class PickerAnalyzer(IMetaCache metaCache) : IPickerAnalyzer
{
    private const decimal W1_ALLIES = 0.7m;
    private const decimal W2_ENEMIES = 1.5m;
    private const decimal W3_BASE_WINRATE = 0.3m;
    private const decimal W4_ROLE_CORRESPONDENCE = 0.5m;
    private const decimal K_SMOOTHING = 800_000m;
    
    public async Task<AnalyzePickResponse> Handle(AnalyzePickRequest request)
    {
        var excludedHeroIds = new HashSet<int>(request.bannedHeroes ?? []);
        if (request.AllyHeroes != null) excludedHeroIds.UnionWith(request.AllyHeroes);
        if (request.EnemyHeroes != null) excludedHeroIds.UnionWith(request.EnemyHeroes);

        var allyHeroes = request.AllyHeroes ?? [];
        var enemyHeroes = request.EnemyHeroes ?? [];

        var scoredHeroes = metaCache.HeroNames
            .Where(x => !excludedHeroIds.Contains(x.Key))
            .Select(kvp =>
            {
                int heroId = kvp.Key;
                string heroName = kvp.Value;

                _ = metaCache.HeroWinRates.TryGetValue(heroId, out var winRateData);
                _ = metaCache.HeroRoles.TryGetValue(heroId, out var roles);

                int totalMatches = winRateData.mathCount;
                int totalWins = winRateData.winCount;

         
                decimal baseWinRate = totalMatches > 0
                    ? Math.Round((decimal)totalWins / totalMatches * 100m, 2)
                    : 50.0m;

                var alliesBreakdown = allyHeroes
                    .Select(teammateId =>
                    {
                        string teammateName = metaCache.HeroNames.GetValueOrDefault(teammateId, $"Hero {teammateId}");
                        if (metaCache.HeroTeamMatchUps.TryGetValue((heroId, teammateId), out var matchUp))
                        {
                            return new HeroSynergyDetailDto(
                                teammateId, 
                                teammateName, 
                                Math.Round((decimal)matchUp.Synergy, 2)
                            );
                        }
                        return null;
                    })
                    .Where(s => s != null)
                    .Select(s => s!)
                    .ToList();

                decimal avgAllySynergy = alliesBreakdown.Count > 0 
                    ? alliesBreakdown.Average(x => x.Synergy) 
                    : 0m;

             
                decimal confidence = totalMatches > 0
                    ? (decimal)totalMatches / (totalMatches + K_SMOOTHING)
                    : 0m;

                decimal smoothedSynergy = avgAllySynergy * confidence;

                var percentForSelectedRole = metaCache.HeroRoles.Where(x => x.Key == heroId).Select(x =>
                {
                    return request.SelectedRoleNumber switch
                    {
                        1 => x.Value.CarryMatchesPercent,
                        2 => x.Value.MidMatchesPercent,
                        3 => x.Value.OfflaneMatchesPercent,
                        4 => x.Value.SoftSupportMatchesPercent,
                        5 => x.Value.HardSupportMatchesPercent,
                        _ => throw new NotImplementedException("Unknown role number")
                    };
                }).FirstOrDefault();


                var finalPercentage = Math.Sqrt((double)Math.Max(percentForSelectedRole - 20m,0m));
                
                
             
                var enemiesBreakdown = enemyHeroes
                    .Select(enemyId =>
                    {
                        string enemyName = metaCache.HeroNames.GetValueOrDefault(enemyId, $"Hero {enemyId}");
                        if (metaCache.HeroVsMatchUps.TryGetValue((heroId, enemyId), out var vsScore))
                        {
                       
                            decimal vsWinRate = Math.Round((decimal)vsScore * 100m, 2);
                            decimal advVsHero = Math.Round(vsWinRate - baseWinRate, 2);

                            return new HeroMatchupDetailDto(
                                enemyId, 
                                enemyName, 
                                vsWinRate, 
                                advVsHero
                            );
                        }
                        return null;
                    })
                    .Where(s => s != null)
                    .Select(s => s!)
                    .ToList();

                decimal avgEnemyWinRate = enemiesBreakdown.Count > 0
                    ? enemiesBreakdown.Average(x => x.WinRateVsEnemy)
                    : baseWinRate;

                decimal advantageVsEnemies = Math.Min((avgEnemyWinRate - baseWinRate), 6) * confidence;

                
                decimal baseWinRateDelta = baseWinRate - 50.0m;
                
                

                
                decimal finalScore = (W1_ALLIES * smoothedSynergy)
                                   + (W2_ENEMIES * advantageVsEnemies)
                                   + (W3_BASE_WINRATE * baseWinRateDelta) + (W4_ROLE_CORRESPONDENCE * baseWinRateDelta);

                return new HeroRecommendationDto
                {
                    HeroId = heroId,
                    Name = heroName,
                    WinCount = totalWins,
                    MatchCount = totalMatches,
                    AllySynergy = Math.Round(avgAllySynergy, 2),
                    WinRate = baseWinRate,
                    LineMatchPercentage = percentForSelectedRole,
                    EnemyAdvantage = Math.Round(advantageVsEnemies, 2),
                    FinalScore = Math.Round(finalScore, 2),
                    AlliesBreakdown = alliesBreakdown,
                    EnemiesBreakdown = enemiesBreakdown
                };
            })
            .Where(x=>x.LineMatchPercentage >= 20)
            .OrderByDescending(x => x.FinalScore)
            .ToList();

        var response = new AnalyzePickResponse(scoredHeroes);
        return response;
    }
    
}

public record HeroSynergyDetailDto(
    int HeroId,
    string HeroName,
    decimal Synergy
);

public record HeroMatchupDetailDto(
    int HeroId,
    string HeroName,
    decimal WinRateVsEnemy,
    decimal Advantage
);

public record HeroRecommendationDto
{
    public int HeroId { get; init; }
    public string Name { get; init; } = string.Empty;
    public int WinCount { get; init; }
    public int MatchCount { get; init; }
    public decimal AllySynergy { get; init; }
    
    public decimal LineMatchPercentage { get; init; }
    
    public decimal WinRate { get; init; }
    public decimal EnemyAdvantage { get; init; }
    public decimal FinalScore { get; init; }
    
    public List<HeroSynergyDetailDto> AlliesBreakdown { get; init; } = [];
    public List<HeroMatchupDetailDto> EnemiesBreakdown { get; init; } = [];
}