//using Windows.Media.Playback;
//using Windows.Storage;
//using Mediator.Contracts.Services;
//using Windows.Media.Core;

//namespace Mediator.Services;

//internal class AudioService : IAudioService, IDisposable
//{
//    private readonly MediaPlayer _mediaPlayer;

//    public AudioService()
//    {
//        _mediaPlayer = new MediaPlayer();
//        InitializeAudioPlayerAsync();
//    }

//    private async void InitializeAudioPlayerAsync()
//    {
//        var file = await StorageFile.GetFileFromApplicationUriAsync(new Uri("ms-appx:///Assets/alert2.wav"));
//        var stream = await file.OpenAsync(FileAccessMode.Read);
//        _mediaPlayer.Source = MediaSource.CreateFromStream(stream, file.ContentType);
//    }

//    public void Alert()
//    {
//        //_mediaPlayer.Play();
//    }

//    public void Dispose()
//    {
//        _mediaPlayer.Dispose();
//    }
//}