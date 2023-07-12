using System.Collections.Specialized;
using System.Text;
using System.Web;
using Common.Entities;
using Mediator.Contracts.Services;
using Microsoft.Windows.AppNotifications;
using Windows.Data.Xml.Dom;
using Windows.UI.Notifications;

namespace Mediator.Services;

public class AppNotificationService : IAppNotificationService
{
    //private readonly INavigationService _navigationService;

    //public AppNotificationService()
    //{
    //    //_navigationService = navigationService; //INavigationService navigationService
    //}

    ~AppNotificationService()
    {
        Unregister();
    }

    public void Initialize()
    {
        AppNotificationManager.Default.NotificationInvoked += OnNotificationInvoked;
        AppNotificationManager.Default.Register();
    }

    public static void OnNotificationInvoked(AppNotificationManager sender, AppNotificationActivatedEventArgs args)
    {
        // TODO: Handle notification invocations when your app is already running.

        //// // Navigate to a specific page based on the notification arguments.
        //// if (ParseArguments(args.Argument)["action"] == "Settings")
        //// {
        ////    App.MainWindow.DispatcherQueue.TryEnqueue(() =>
        ////    {
        ////        _navigationService.NavigateTo(typeof(SettingsViewModel).FullName!);
        ////    });
        //// }

        //App.MainWindow.DispatcherQueue.TryEnqueue(() =>
        //{
        //    App.MainWindow.ShowMessageDialogAsync("TODO: Handle notification invocations when your app is already running.", "Notification Invoked");

        //    App.MainWindow.BringToFront();
        //});
    }

    public bool Show(string payload)
    {
        var appNotification = new AppNotification(payload);
        AppNotificationManager.Default.Show(appNotification);
        return appNotification.Id != 0;
    }

    public void ShowBackupResultToast(Dictionary<ActionResult, int> result)
    {
        var toastXml = ToastNotificationManager.GetTemplateContent(ToastTemplateType.ToastText01);
        var notificationMessage = new StringBuilder();
        foreach (var actionResult in result.Where(actionResult => actionResult.Value != 0))
        {
            notificationMessage.Append($"{actionResult.Key}: {actionResult.Value} databases, ");
        }

        if (notificationMessage.Length > 0)
        {
            notificationMessage.Length -= 2;
        }

        var textNodes = toastXml.GetElementsByTagName("text");
        textNodes[0].AppendChild(toastXml.CreateTextNode(notificationMessage.ToString()));

        var serializer = toastXml as IXmlNodeSerializer;
        var payload = serializer.GetXml();

        Show(payload!);
    }

    public NameValueCollection ParseArguments(string arguments)
    {
        return HttpUtility.ParseQueryString(arguments);
    }

    public void Unregister()
    {
        AppNotificationManager.Default.Unregister();
    }

    public void ShowMessage(string message)
    {
        var toastXml = ToastNotificationManager.GetTemplateContent(ToastTemplateType.ToastText01);

        var textNodes = toastXml.GetElementsByTagName("text");
        textNodes[0].AppendChild(toastXml.CreateTextNode(message));

        var serializer = toastXml as IXmlNodeSerializer;
        var payload = serializer.GetXml();

        Show(payload!);
    }
}
