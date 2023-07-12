using Mediator.Views;

namespace Mediator;

public sealed partial class MainWindow
{
    public MainWindow()
    {
        InitializeComponent();
        //ExtendsContentIntoTitleBar = true; // doesn't work
        //SetTitleBar(TitleBar); // doesn't work
        Content = null;
    }

    //private AppTitleBar TitleBar => AppTitleBar; // doesn't work
}
