using System.Text;
using Common.ExtensionsAndHelpers;
using Mediator.Contracts.Services;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Mediator.Services;

public class FileService : IFileService
{
    private readonly ILogger<IFileService> _logger;

    public FileService(ILogger<IFileService> logger)
    {
        _logger = logger;
    }

    public T Read<T>(string folderPath, string fileName)
    {
        try
        {
            var path = Path.Combine(folderPath, fileName);
            if (!File.Exists(path))
            {
                return default!;
            }

            var json = File.ReadAllText(path);
            return JsonConvert.DeserializeObject<T>(json)!;
        }
        catch (Exception exception)
        {
            LogExceptionHelper.LogException(_logger, exception, "");
            throw;
        }
    }

    public void Save<T>(string folderPath, string fileName, T content)
    {
        try
        {
            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }

            var fileContent = JsonConvert.SerializeObject(content);
            File.WriteAllText(Path.Combine(folderPath, fileName), fileContent, Encoding.UTF8);
        }
        catch (Exception exception)
        {
            LogExceptionHelper.LogException(_logger, exception, "");
            throw;
        }
    }

    public void Delete(string folderPath, string fileName)
    {
        if (File.Exists(Path.Combine(folderPath, fileName)))
        {
            File.Delete(Path.Combine(folderPath, fileName));
        }
    }
}
