using Microsoft.UI.Xaml.Controls;
using Terminal.WinUI3.ViewModels;

namespace Terminal.WinUI3.Views;

// TODO: Change the grid as appropriate for your app. Adjust the column definitions on DataGridPage.xaml.
// For more details, see the documentation at https://docs.microsoft.com/windows/communitytoolkit/controls/datagrid.
public sealed partial class DataGridPage : Page
{
    public DataGridPage()
    {
        ViewModel = App.GetService<DataGridViewModel>();
        InitializeComponent();
    }

    public DataGridViewModel ViewModel
    {
        get;
    }
}