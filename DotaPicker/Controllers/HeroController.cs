using System.Text.Json;
using MediatR;
using Microsoft.AspNetCore.DataProtection.Internal;
using Microsoft.AspNetCore.Mvc;
using webhook_gateway.Commands;
using webhook_gateway.Interfaces;

namespace webhook_gateway.Controllers;


[ApiController]
[Route("[controller]")]
public class HeroController(IMetaCache metaCache, IMediator mediator) : ControllerBase
{
   [HttpGet("get-hero-winrates")]
   public async Task<IActionResult> GetHeroWinRates()
   {
      var data = metaCache.HeroWinRates.ToDictionary(x => x.Key, x => new
      {
         winCount = x.Value.winCount,
         mathCount = x.Value.mathCount,
      }).Select(x=> new
      {
         HeroId = x.Key,
         DisplayName = metaCache.HeroNames[x.Key],
         winRate = Math.Round(((double)x.Value.winCount/(double)x.Value.mathCount)*100,2),
      });
      return Ok(data);
   }

   [HttpGet("get-all-hero-names")]
   public IActionResult GetHeroNames()
   {
      var data = metaCache.HeroNames;
      return Ok(data);
   }

   [HttpPost("get-analytics")]
   public async Task<IActionResult> GetAnalytics([FromBody] AnalyzePickRequest request, CancellationToken cancellationToken)
   {
      var result = await mediator.Send(request, cancellationToken);
      return Ok(result);
   }
}