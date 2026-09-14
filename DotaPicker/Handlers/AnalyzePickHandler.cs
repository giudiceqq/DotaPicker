using MediatR;
using webhook_gateway.Commands;
using webhook_gateway.DTO;
using webhook_gateway.Interfaces;

namespace webhook_gateway.Handlers;

public class AnalyzePickHandler(IMetaCache metaCache, IPickerAnalyzer pickerAnalyzer) : IRequestHandler<AnalyzePickRequest, AnalyzePickResponse>
{

    public async Task<AnalyzePickResponse> Handle(AnalyzePickRequest request, CancellationToken cancellationToken)
    {
        var result = await pickerAnalyzer.Handle(request);
        return result;
    }

}



