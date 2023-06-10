using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Terminal.WinUI3.ViewModels;

public sealed class DialogViewModel : INotifyPropertyChanged
{
    private string _infoMessage = null!;
    public string InfoMessage
    {
        get => _infoMessage;
        set
        {
            if (_infoMessage == value)
            {
                return;
            }

            _infoMessage = value;
            OnPropertyChanged();
        }
    }

    //private string _progressMessage = null!;
    //public string ProgressMessage
    //{
    //    get => _progressMessage;
    //    set
    //    {
    //        if (_progressMessage == value)
    //        {
    //            return;
    //        }

    //        _progressMessage = value;
    //        OnPropertyChanged();
    //    }
    //}

    private int _progressPercentage;
    public int ProgressPercentage
    {
        get => _progressPercentage;
        set
        {
            if (_progressPercentage == value)
            {
                return;
            }

            _progressPercentage = value;
            OnPropertyChanged();
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged([CallerMemberName] string propertyName = null!) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName)); 
}