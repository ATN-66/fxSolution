using System.Reflection;
using NAudio.Wave;

namespace Common.Entities;

public class AudioPlayer : IAudioPlayer
{
    private const string AudioFileName = "alert2.wav";
    private readonly AudioFileReader _audioFileReader;
    private readonly WaveOutEvent _outputDevice;

    public AudioPlayer()
    {
        var assemblyLocation = Assembly.GetExecutingAssembly().Location;
        var directoryPath = Path.GetDirectoryName(assemblyLocation)!;
        var fullAudioFilePath = Path.Combine(directoryPath, AudioFileName);
        _audioFileReader = new AudioFileReader(fullAudioFilePath);
        _outputDevice = new WaveOutEvent();
        _outputDevice.Init(_audioFileReader);
    }

    public void Play()
    {
        //try
        //{
        //    _audioFileReader.Position = 0;
        //    _outputDevice.Play();
        //}
        //catch (Exception)
        //{
        //    // ignored
        //}
    }

    public void Stop()
    {
        _outputDevice.Stop();
    }

    public void Dispose()
    {
        _outputDevice?.Dispose();
        _audioFileReader?.Dispose();
    }
}