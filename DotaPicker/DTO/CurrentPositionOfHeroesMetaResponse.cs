using System.Text.Json.Serialization;

namespace webhook_gateway.DTO;



public record CurrentPositionData(CurrentPositionOfHeroesMetaResponse data);

public record CurrentPositionOfHeroesMetaResponse
(
    [property: JsonPropertyName("result")] PositionResult result 
);

public record PositionResult( [property: JsonPropertyName("total")] Dictionary<string, int> TotalMatches
,[property: JsonPropertyName("1.1")] Dictionary<int, HeroStatisticForEachRole> CarryPicks
,[property: JsonPropertyName("1.2")] Dictionary<int, HeroStatisticForEachRole> MidPicks
,[property: JsonPropertyName("1.3")] Dictionary<int, HeroStatisticForEachRole> OfflanePicks
,[property: JsonPropertyName("0.3")] Dictionary<int, HeroStatisticForEachRole> SoftSupport
,[property: JsonPropertyName("0.1")] Dictionary<int, HeroStatisticForEachRole> HardSupport);

public record HeroStatisticForEachRole(int matches_s);

   