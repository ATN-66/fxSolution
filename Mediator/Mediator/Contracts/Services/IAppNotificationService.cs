using System.Collections.Specialized;
using Common.Entities;

namespace Mediator.Contracts.Services;

public interface IAppNotificationService
{
    void Initialize();

    bool Show(string payload);
    void ShowBackupResultToast(Dictionary<ActionResult, int> result);
    NameValueCollection ParseArguments(string arguments);
    void Unregister();
    void ShowMessage(string indicatorsConnected);
}
