using webhook_gateway.DTO;

namespace webhook_gateway.Interfaces;

public interface IMetaDataFetcher
{
    Task<StratzRootResponse?> FetchMetaDataAsync(CancellationToken cancellationToken);
    
    Task<CurrentPositionData> FetchMetaDataOfHeroRolesAsync(CancellationToken cancellationToken);
    
}