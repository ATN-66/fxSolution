namespace Mediator.Contracts.Services;

public interface IExecutiveProviderService
{
    Task StartAsync();
    void DeInitAsync(string dateTime);
    Task<string> InitAsync(string datetime);
    Task<string> PulseAsync(string dateTime, string type, string code, string ticket, string details);
}