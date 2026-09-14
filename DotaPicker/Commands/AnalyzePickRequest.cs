using MediatR;
using webhook_gateway.DTO;
using webhook_gateway.Handlers;
using webhook_gateway.Services;

namespace webhook_gateway.Commands;

public record AnalyzePickRequest(int SelectedRoleNumber, List<int> AllyHeroes, List<int>  EnemyHeroes, List<int> bannedHeroes, int? SelectedHeroId) : IRequest<AnalyzePickResponse>;

public record AnalyzePickResponse(List<HeroRecommendationDto> heroStats);