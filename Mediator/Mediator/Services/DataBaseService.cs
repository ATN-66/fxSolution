/*+------------------------------------------------------------------+
  |                                                 Mediator.Services|
  |                                                   DataBaseService.cs |
  +------------------------------------------------------------------+*/

using Mediator.Contracts.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Common.DataSource;
using System.Reflection;
using Common.Entities;

namespace Mediator.Services;

public class DataBaseService : DataBaseSource, IDataBaseService
{
    public DataBaseService(IConfiguration configuration, IOptions<ProviderBackupSettings> providerBackupSettings, ILogger<IDataSource> logger, IAudioPlayer audioPlayer) : base(configuration,  providerBackupSettings, logger, audioPlayer)
    {
        Workplace = SetWorkplaceFromEnvironment(Assembly.GetExecutingAssembly().GetName().Name!);
    }
}