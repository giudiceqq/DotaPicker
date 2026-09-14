using webhook_gateway.Commands;

namespace webhook_gateway.Interfaces;

public interface IPickerAnalyzer
{
    Task<AnalyzePickResponse> Handle(AnalyzePickRequest request);
}