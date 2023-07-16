using System.Collections.Concurrent;
using System.Globalization;
using System.Text;
using Common.Entities;
using Common.ExtensionsAndHelpers;
using Fx.Grpc;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Mediator.Contracts.Services;
using Mediator.Models;
using Mediator.ViewModels;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Enum = System.Enum;

namespace Mediator.Services;

public class ExecutiveProviderService : ExecutiveProvider.ExecutiveProviderBase, IExecutiveProviderService
{
    private const string Ok = "ok";
    private readonly Guid _guid = Guid.NewGuid();
    private readonly MainViewModel _mainViewModel;
    private readonly CancellationTokenSource _cts;
    private readonly ILogger<IDataProviderService> _logger;
    private readonly ExecutiveProviderSettings _executiveProviderSettings;
    private readonly StringBuilder _messageBuilder = new();

    private readonly string _mT5DateTimeFormat;
    private DateTime _currentDateTime;
    private readonly ConcurrentQueue<string> _outcomeMessages = new();
    private IServerStreamWriter<GeneralResponse>? _responseStream;

    private static readonly string[] MessageTypeNames = Enum.GetNames(typeof(MessageType));
    private static readonly string[] AccountInfoCodeNames = Enum.GetNames(typeof(AccountInfoCode));
    private static readonly string[] TradeCodeNames = Enum.GetNames(typeof(TradeCode));

    private const int RetryCountLimit = 5;
    private const int Backoff = 5;
    private readonly int _maxSendMessageSize;
    private readonly int _maxReceiveMessageSize;
#pragma warning disable CS0414
    private static volatile bool _isFaulted;
#pragma warning restore CS0414

    private ServiceStatus _serviceStatus;
    private ClientStatus _clientStatus;

    public event EventHandler<ServiceStatusChangedEventArgs> ServiceStatusChanged = null!;
    public event EventHandler<ClientStatusChangedEventArgs> ClientStatusChanged = null!;

    public ExecutiveProviderService(IConfiguration configuration, IOptions<ExecutiveProviderSettings> executiveProviderSettings, MainViewModel mainViewModel, CancellationTokenSource cts, ILogger<IDataProviderService> logger)
    {
        _mainViewModel = mainViewModel;
        ServiceStatusChanged += (_, e) => { _mainViewModel.ExecutiveProviderServiceStatus = e.ServiceStatus; };
        ClientStatusChanged += (_, e) => { _mainViewModel.ExecutiveProviderClientStatus = e.ClientStatus; };
        _cts = cts;
        _logger = logger;

        _mT5DateTimeFormat = configuration.GetValue<string>($"{nameof(_mT5DateTimeFormat)}")!;
        _maxSendMessageSize = configuration.GetValue<int>($"{nameof(_maxSendMessageSize)}");
        _maxReceiveMessageSize = configuration.GetValue<int>($"{nameof(_maxReceiveMessageSize)}");
        _executiveProviderSettings = executiveProviderSettings.Value;

        _serviceStatus = ServiceStatus.Off;
        _clientStatus = ClientStatus.Off;

        _logger.LogTrace("{_guid} is ON. {_executiveProviderHost}:{_executiveProviderPort}", _guid, _executiveProviderSettings.Host, _executiveProviderSettings.Port);
    }
    
    private ServiceStatus ServiceStatus
    {
        get => _serviceStatus;
        set
        {
            if (value == _serviceStatus)
            {
                return;
            }

            _serviceStatus = value;
            ServiceStatusChanged(this, new ServiceStatusChangedEventArgs(ServiceStatus));
        }
    }
    private ClientStatus ClientStatus
    {
        get => _clientStatus;
        set
        {
            if (value == _clientStatus)
            {
                return;
            }

            _clientStatus = value;
            ClientStatusChanged(this, new ClientStatusChangedEventArgs(ClientStatus));
        }
    }

    public async Task StartAsync()
    {
        var retryCount = 0;

        while (retryCount < RetryCountLimit && !_cts.Token.IsCancellationRequested)
        {
            Server? grpcServer = null;
            _isFaulted = false;

            try
            {
                grpcServer = new Server(new List<ChannelOption>
                {
                    new("grpc.max_send_message_length", _maxSendMessageSize),
                    new("grpc.max_receive_message_length", _maxReceiveMessageSize)

                })
                {
                    Services = { ExecutiveProvider.BindService(this) },
                    Ports = { new ServerPort(_executiveProviderSettings.Host, _executiveProviderSettings.Port, ServerCredentials.Insecure) },
                };
                grpcServer.Start();

                ServiceStatus = ServiceStatus.On;
                _logger.LogTrace("Listening on {host}:{port}", _executiveProviderSettings.Host, _executiveProviderSettings.Port.ToString());
                await Task.Delay(Timeout.Infinite, _cts.Token).ConfigureAwait(false);

                retryCount = 0;
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Operation cancelled, shutting down executive provider gRPC server...");
                break;  // Break out of the loop on cancellation, assuming we don't want to retry in this case
            }
            catch (IOException ioException)
            {
                LogExceptionHelper.LogException(_logger, ioException, "");
                retryCount++;
            }
            catch (Exception exception)
            {
                LogExceptionHelper.LogException(_logger, exception, "");
                retryCount++;
            }
            finally
            {
                if (grpcServer != null)
                {
                    ServiceStatus = ServiceStatus.Off;
                    await grpcServer.ShutdownAsync().ConfigureAwait(false);
                    _logger.LogTrace("executive provider gRPC Server shutted down.");
                }
            }

            if (retryCount > 0)
            {
                await Task.Delay(TimeSpan.FromSeconds(Backoff * retryCount)).ConfigureAwait(false);
            }
        }

        if (retryCount >= RetryCountLimit)
        {
            _mainViewModel.AtFault = true;
            _logger.LogCritical("CRITICAL ERROR: The executive provider gRPC server has repeatedly failed to start after {retryCount} attempts. This indicates a severe underlying issue that needs immediate attention. The server will not try to restart again. Please check the error logs for more information and take necessary action immediately.", retryCount);
        }
    }

    public void DeInitAsync(string dateTime)
    {
        _currentDateTime = DateTime.ParseExact(dateTime, _mT5DateTimeFormat, CultureInfo.InvariantCulture);
    }
    public Task<string> InitAsync(string dateTime)
    {
        _currentDateTime = DateTime.ParseExact(dateTime, _mT5DateTimeFormat, CultureInfo.InvariantCulture);
        return Task.FromResult(Ok);
    }
    public string Pulse(string dateTime, string type, string code, string ticket, string result, string details)
    {
        _currentDateTime = DateTime.ParseExact(dateTime, _mT5DateTimeFormat, CultureInfo.InvariantCulture);
        GeneralResponse response;
        string? outcomeResult;
        switch (type)
        {
            case "0":
                return code switch
                {
                    "0" => _outcomeMessages.TryDequeue(out outcomeResult) ? outcomeResult : Ok,
                    _ => throw new ArgumentOutOfRangeException(nameof(code), @"Pulse: The provided string code is not supported.")
                };
            case "TradeCommand":
                response = new GeneralResponse
                {
                    Type = MessageType.TradeCommand,
                    TradeResponse = new TradeResponse()
                    {
                        TradeCode = code switch
                        {
                            "OpenPosition" => TradeCode.OpenPosition,
                            "ClosePosition" => TradeCode.ClosePosition,
                            "OpenTransaction" => TradeCode.OpenTransaction,
                            "CloseTransaction" => TradeCode.CloseTransaction,
                            "UpdatePosition" => TradeCode.UpdatePosition,
                            _ => throw new ArgumentOutOfRangeException(nameof(code), @"Pulse: The provided string TradeCode is not supported.")
                        },
                        Ticket = int.Parse(ticket),
                        ResultCode = result switch
                        {
                            "FAILURE" => ResultCode.Failure,
                            "SUCCESS" => ResultCode.Success,
                            _ => throw new ArgumentOutOfRangeException(nameof(code), @"Pulse: The provided string ResultCode is not supported.")
                        },
                        Details = details
                    }
                };
                try
                {
                    _responseStream?.WriteAsync(response).ConfigureAwait(false);
                }
                catch (RpcException)
                {
                    _responseStream = null;
                    // ignore
                    var st = true;
                }
                catch (Exception exception)
                {
                    _responseStream = null;
                    LogExceptionHelper.LogException(_logger, exception, "");
                }
                return _outcomeMessages.TryDequeue(out outcomeResult) ? outcomeResult : Ok;
            case "AccountInfo":
                response = new GeneralResponse
                {
                    Type = MessageType.AccountInfo,
                    AccountInfoResponse = new AccountInfoResponse
                    {
                        AccountInfoCode = code switch
                        {
                            "AccountProperties" => AccountInfoCode.AccountProperties,
                            "TradingHistory" => AccountInfoCode.TradingHistory,
                            _ => throw new ArgumentOutOfRangeException(nameof(code), @"Pulse: The provided string AccountInfoCode is not supported.")
                        },
                        Ticket = int.Parse(ticket),
                        ResultCode = result switch
                        {
                            "FAILURE" => ResultCode.Failure,
                            "SUCCESS" => ResultCode.Success,
                            _ => throw new ArgumentOutOfRangeException(nameof(code), @"Pulse: The provided string ResultCode is not supported.")
                        },
                        Details = details
                    }
                };
                try
                {
                    _responseStream?.WriteAsync(response).ConfigureAwait(false);
                }
                catch (Exception exception)
                {
                    _responseStream = null;
                    LogExceptionHelper.LogException(_logger, exception, "");
                    throw;
                }
                return _outcomeMessages.TryDequeue(out outcomeResult) ? outcomeResult : Ok;
            default: throw new ArgumentOutOfRangeException(nameof(type), @"PulseAsync: The provided string type is not supported.");
        }
    }

    public async override Task CommunicateAsync(IAsyncStreamReader<GeneralRequest> requestStream, IServerStreamWriter<GeneralResponse> responseStream, ServerCallContext context)
    {
        if (ServiceStatus != ServiceStatus.On)
        {
            return;
        }
        ClientStatus = ClientStatus.On;

        _responseStream = responseStream;

        try
        {
            while (await requestStream.MoveNext(context.CancellationToken).ConfigureAwait(false))
            {
                if (context.CancellationToken.IsCancellationRequested) { break; }
                var request = requestStream.Current;
                string typeName;
                string codeName;
                int ticket;
                switch (request.Type)
                {
                    case MessageType.MaintenanceCommand:
                        switch (request.MaintenanceRequest.MaintenanceCode)
                        {
                            case MaintenanceCode.OpenSession:
                                var response = new GeneralResponse { Type = MessageType.MaintenanceCommand, MaintenanceResponse = new MaintenanceResponse() };
                                response.MaintenanceResponse.ResultCode = ResultCode.Success;
                                response.MaintenanceResponse.Datetime = Timestamp.FromDateTime(_currentDateTime.ToUniversalTime());
                                await responseStream.WriteAsync(response).ConfigureAwait(false);
                                break;
                            case MaintenanceCode.CloseSession:
                                // clean up
                                ClientStatus = ClientStatus.Off;
                                _responseStream = null!;
                                return;
                            default: throw new ArgumentOutOfRangeException($"{request.MaintenanceRequest.MaintenanceCode}", @"ExecutiveProviderService.CommunicateAsync.CloseSession: The provided request.MaintenanceRequest.MaintenanceCode is not supported.");
                        }
                        break;
                    case MessageType.AccountInfo:
                        typeName = MessageTypeNames[(int)request.Type];
                        codeName = AccountInfoCodeNames[(int)request.AccountInfoRequest.AccountInfoCode];
                        ticket = request.AccountInfoRequest.Ticket;
                        _messageBuilder.Clear();
                        _messageBuilder.Append("Type: ").Append(typeName).Append(", Code: ").Append(codeName).Append(", Ticket: ").Append(ticket);
                        _messageBuilder.Append(", Details: {").Append(request.AccountInfoRequest.Details).Append('}');
                        _outcomeMessages.Enqueue(_messageBuilder.ToString());
                        break;
                    case MessageType.TradeCommand:
                        typeName = MessageTypeNames[(int)request.Type];
                        codeName = TradeCodeNames[(int)request.TradeRequest.TradeCode];
                        ticket = request.TradeRequest.Ticket;
                        _messageBuilder.Clear();
                        _messageBuilder.Append("Type: ").Append(typeName).Append(", Code: ").Append(codeName).Append(", Ticket: ").Append(ticket);
                        _messageBuilder.Append(", Details: {").Append(request.TradeRequest.Details).Append('}');
                        _outcomeMessages.Enqueue(_messageBuilder.ToString()); 
                        break;
                    default: throw new ArgumentOutOfRangeException($"{request.Type}", @"ExecutiveProviderService.CommunicateAsync: The provided request Type is not supported.");
                }
            }
        }
        catch (Exception)
        {
            if (context.CancellationToken.IsCancellationRequested)
            {
                throw;
            }
            throw;
        }

        ClientStatus = ClientStatus.Off;
    }
}