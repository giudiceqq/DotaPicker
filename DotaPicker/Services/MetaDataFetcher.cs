using System.Text.Json;
using webhook_gateway.DTO;
using webhook_gateway.Interfaces;

namespace webhook_gateway.Services;

public class MetaDataFetcher(IHttpClientFactory clientFactory) : IMetaDataFetcher
{
    public async Task<StratzRootResponse?> FetchMetaDataAsync(CancellationToken cancellationToken)
    {
        var client = clientFactory.CreateClient("StratzClient");
        
        var requestBody = new
        {
            query = """
                    query GetHeroInformation {
                      constants {
                        heroes { id displayName roles { roleId level} }
                      }
                      heroStats {
                        winWeek { heroId winCount matchCount }
                        matchUp(take: 150, bracketBasicIds: [DIVINE_IMMORTAL]) {
                          heroId
                          with { heroId2 synergy winsAverage }
                          vs { heroId2 winsAverage }
                        }
                      }
                    }
                    """
        };
        
        var response = await client.PostAsJsonAsync("", requestBody, cancellationToken);
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<StratzRootResponse>(cancellationToken);
      
        return result;

    }

    public async Task<CurrentPositionData> FetchMetaDataOfHeroRolesAsync(
      CancellationToken cancellationToken)
    {
      var client = clientFactory.CreateClient("stats.spectral.gg");
      // https://stats.spectral.gg
      
      
      var response = await client.GetAsync("https://stats.spectral.gg/lrg2/api/?league=imm_ranked_741e&mod=heroes-positions-table&limit=2000", cancellationToken);
      response.EnsureSuccessStatusCode();
      var result = await response.Content.ReadAsStringAsync(cancellationToken);
      
      var data = JsonSerializer.Deserialize<CurrentPositionOfHeroesMetaResponse>(result);
      return new CurrentPositionData(data);
      

    }
}


