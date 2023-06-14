using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Text;
using Common.Entities;
using Common.ExtensionsAndHelpers;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Terminal.WinUI3.Contracts.Services;
using Windows.Storage;
using Environment = System.Environment;

namespace Terminal.WinUI3.Services;

public class FileService : IFileService
{
    private readonly string[] _formats;
    private readonly string _inputDirectoryPath;
    private readonly Dictionary<string, List<Quotation>> _ticksCache = new();
    private readonly Queue<string> _keys = new();
    private const int MaxItems = 10_000;

    public FileService(IConfiguration configuration)
    {
        _inputDirectoryPath = configuration.GetValue<string>("InputDirectoryPath")!;
        _formats = new[] { configuration.GetValue<string>("DateTimeFormat")! };
    }

    public async Task<IEnumerable<Quotation>> GetTicksAsync(DateTime startDateTime, DateTime endDateTime)
    {
        var result = new List<Quotation>();
        var start = startDateTime.Date.AddHours(startDateTime.Hour);
        var end = endDateTime.Date.AddHours(endDateTime.Hour);
        var timeDifference = end - start;

        if (timeDifference.TotalHours <= 24)
        {
            for (var index = start; index < end; index = index.Add(new TimeSpan(1, 0, 0)))
            {
                var key = $"{index.Year}.{index.Month:D2}.{index.Day:D2}.{index.Hour:D2}";

                if (!_ticksCache.ContainsKey(key))
                {
                    await LoadTicksToCacheAsync(index).ConfigureAwait(false);
                }

                result.AddRange(GetQuotations(key));
            }
        }
        else
        {
            throw new NotImplementedException();//never wea debugged. check how it works
            //var loadTasks = new List<Task>();
            //for (var index = start; index <= end; index = index.Add(new TimeSpan(1, 0, 0)))
            //{
            //    var key = $"{index.Year}.{index.Month:D2}.{index.Day:D2}.{index.Hour:D2}";

            //    if (!_ticksCache.ContainsKey(key))
            //    {
            //        loadTasks.Add(LoadTicksToCacheAsync(index));
            //    }
            //}
            //await Task.WhenAll(loadTasks).ConfigureAwait(false);

            //for (var index = start; index <= end; index = index.Add(new TimeSpan(1, 0, 0)))
            //{
            //    var key = $"{index.Year}.{index.Month:D2}.{index.Day:D2}.{index.Hour:D2}";
            //    result.AddRange(GetQuotations(key));
            //}
        }

        return result;
    }

    private async Task LoadTicksToCacheAsync(DateTime dateTime)
    {
        var symbolsDict = Enum.GetValues<Symbol>().ToDictionary(e => e.ToString(), e => e);
        var year = dateTime.Year.ToString();
        var month = dateTime.Month.ToString("D2");
        var week = dateTime.Week().ToString("D2");
        var quarter = DateTimeExtensionsAndHelpers.Quarter(dateTime.Week()).ToString();
        var day = dateTime.Day.ToString("D2");
        var hour = dateTime.Hour.ToString("D2");
        var key = $"{year}.{month}.{day}.{hour}";

        var directoryPath = Path.Combine(_inputDirectoryPath, year, quarter, week, day);
        var filePath = Path.Combine(directoryPath, $"{key}.csv");
        if (!File.Exists(filePath))
        {
            SetQuotations(key, new List<Quotation>());
            return;
        }

        var lines = await File.ReadAllLinesAsync(filePath).ConfigureAwait(true);
        var quotations = lines.AsParallel()
            .Select(line => line.Split('|').Select(str => str.Trim()).ToArray())
            .Select((items, index) =>
            {
                try
                {
                    var quotationId = index + 1;
                    var symbolResult = symbolsDict[items[0]];
                    var datetimeString = $"{items[1].Trim()}|{items[2].Trim()}|{items[3].Trim()}";
                    var dateTimeResult = DateTime.ParseExact(datetimeString, _formats, new CultureInfo("en-US"), DateTimeStyles.AssumeUniversal).ToUniversalTime();
                    var askResult = double.Parse(items[4]);
                    var bidResult = double.Parse(items[5]);
                    return new Quotation(quotationId, symbolResult, dateTimeResult, askResult, bidResult);
                }
                catch (FormatException ex)
                {
                    Debug.WriteLine(ex.Message);
                    Debug.WriteLine($@"Failed to parse datetime string: {items[1]}, {items[2]}, {items[3]}");
                    throw;
                }
            })
            .OrderBy(quotation => quotation.DateTime)
            .ToList();
        
        SetQuotations(key, quotations);
    }

    private void AddQuotation(string key, List<Quotation> quotationList)
    {
        if (_ticksCache.Count >= MaxItems)
        {
            var oldestKey = _keys.Dequeue();
            _ticksCache.Remove(oldestKey);
        }

        _ticksCache[key] = quotationList;
        _keys.Enqueue(key);
    }

    private void SetQuotations(string key, List<Quotation> quotations)
    {
        AddQuotation(key, quotations);
    }

    private IEnumerable<Quotation> GetQuotations(string key)
    {
        _ticksCache.TryGetValue(key, out var quotations);
        return quotations!;
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