namespace Mediator.Contracts.Services;

public interface IExecutiveProviderService
{
    Task StartAsync();
    void DeInitAsync(string dateTime);
    Task<string> InitAsync(string datetime);
    string Pulse(string dateTime, int type, int code, string message);
}