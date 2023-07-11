/*+------------------------------------------------------------------+
  |                                Terminal.WinUI3.Contracts.Services|
  |                                                     IDataConsumerService.cs |
  +------------------------------------------------------------------+*/

using System.Collections.Concurrent;
using Common.DataSource;
using Common.Entities;
using Grpc.Net.Client;

namespace Terminal.WinUI3.Contracts.Services;

public interface IDataConsumerService : IDataSource
{
    Task<(Task, GrpcChannel)> StartAsync(BlockingCollection<Quotation> quotations, CancellationToken token);
    Task<IList<Quotation>> GetBufferedDataAsync(CancellationToken token);
}
