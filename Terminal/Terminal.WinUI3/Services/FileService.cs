/*+------------------------------------------------------------------+
  |                                          Terminal.WinUI3.Services|
  |                                                   FileService.cs |
  +------------------------------------------------------------------+*/

using System.Globalization;
using System.Reflection;
using System.Text;
using Common.DataSource;
using Common.Entities;
using Common.ExtensionsAndHelpers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Terminal.WinUI3.Contracts.Services;
using Windows.Storage;
using Environment = System.Environment;

namespace Terminal.WinUI3.Services;

public class FileService : DataSource, IFileService
{
    private readonly string _fileServiceDateTimeFormat;
    private readonly string _fileServiceInputDirectoryPath;
    private readonly Dictionary<string, Symbol> _symbolsDict = Enum.GetValues<Symbol>().ToDictionary(e => e.ToString(), e => e);

    public FileService(IConfiguration configuration, ILogger<IDataSource> logger, IAudioPlayer audioPlayer) : base(configuration, logger, audioPlayer)
    {
        _fileServiceInputDirectoryPath = configuration.GetValue<string>($"{nameof(_fileServiceInputDirectoryPath)}")!;
        _fileServiceDateTimeFormat = configuration.GetValue<string>($"{nameof(_fileServiceDateTimeFormat)}")!;
    }

    protected async override Task<IList<Quotation>> GetDataAsync(DateTime startDateTimeInclusive)
    {
        var year = startDateTimeInclusive.Year.ToString();
        var month = startDateTimeInclusive.Month.ToString("D2");
        var week = startDateTimeInclusive.Week().ToString("D2");
        var quarter = DateTimeExtensionsAndHelpers.Quarter(startDateTimeInclusive.Week()).ToString();
        var day = startDateTimeInclusive.Day.ToString("D2");
        var hour = startDateTimeInclusive.Hour.ToString("D2");

        var key = $"{year}.{month}.{day}.{hour}";

        var directoryPath = Path.Combine(_fileServiceInputDirectoryPath, year, quarter, week, day);
        var filePath = Path.Combine(directoryPath, $"{key}.csv");
        if (!File.Exists(filePath))
        {
            return new List<Quotation>();
        }

        var lines = await File.ReadAllLinesAsync(filePath).ConfigureAwait(true);
        var quotations = lines.AsParallel()
            .Select(line => line.Split('|').Select(str => str.Trim()).ToArray())
            .Select((items, index) =>
            {
                try
                {
                    var quotationId = index + 1;
                    var symbolResult = _symbolsDict[items[0]];
                    var datetimeString = $"{items[1].Trim()}|{items[2].Trim()}|{items[3].Trim()}";
                    var dateTimeResult = DateTime.ParseExact(datetimeString, _fileServiceDateTimeFormat, new CultureInfo("en-US"), DateTimeStyles.AssumeUniversal).ToUniversalTime();
                    var askResult = double.Parse(items[4]);
                    var bidResult = double.Parse(items[5]);
                    return new Quotation(quotationId, symbolResult, dateTimeResult, askResult, bidResult);
                }
                catch (FormatException formatException)
                {
                    LogExceptionHelper.LogException(_logger, formatException, MethodBase.GetCurrentMethod()!.Name, "");
                    throw;
                }
            })
            .OrderBy(quotation => quotation.DateTime)
            .ToList();

        return quotations;
    }
    
    public T? Read<T>(string folderPath, string fileName)
    {
        var path = Path.Combine(folderPath, fileName);
        if (!File.Exists(path))
        {
            return default;
        }

        var json = File.ReadAllText(path);
        return JsonConvert.DeserializeObject<T>(json);
    }
    public void Save<T>(string folderPath, string fileName, T content)
    {
        if (!Directory.Exists(folderPath))
        {
            Directory.CreateDirectory(folderPath);
        }

        var fileContent = JsonConvert.SerializeObject(content);
        File.WriteAllText(Path.Combine(folderPath, fileName), fileContent, Encoding.UTF8);
    }
    public void Delete(string folderPath, string? fileName)
    {
        if (fileName != null && File.Exists(Path.Combine(folderPath, fileName)))
        {
            File.Delete(Path.Combine(folderPath, fileName));
        }
    }
    public async Task<string> LoadTextAsync(string relativeFilePath)
    {
        //#if PACKAGED
        //var sourceUri = new Uri("ms-appx:///" + relativeFilePath);
        //var file = await StorageFile.GetFileFromApplicationUriAsync(sourceUri); 
        //return await FileIO.ReadTextAsync(file);
        var sourcePath = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(Assembly.GetEntryAssembly()?.Location)!, relativeFilePath));
        var file = await StorageFile.GetFileFromPathAsync(sourcePath);
        return await FileIO.ReadTextAsync(file);
    }
    public async Task<IList<string>> LoadLinesAsync(string relativeFilePath)
    {
        var fileContents = await LoadTextAsync(relativeFilePath).ConfigureAwait(false);
        return fileContents.Split(Environment.NewLine).ToList();
    }
}