namespace webhook_gateway.DTO;

public record StratzRootResponse(StratzMetaResponse data);

public record StratzMetaResponse(ConstantsDto constants, HeroStatsDto heroStats);

public record ConstantsDto(List<HeroDto> heroes);

public record HeroDto(int id, string displayName, List<Role> roles);

public record Role(string roleId, int level);

public record HeroStatsDto(List<WinWeekDto> winWeek, List<MatchUpDto> matchUp);

public record WinWeekDto(int heroId, int winCount, int matchCount);

public record MatchUpDto(int heroId, List<WithDto> with, List<VsDto> vs);



public record WithDto(int heroId2, double synergy,double winsAverage);
public record VsDto(int heroId2, double winsAverage);