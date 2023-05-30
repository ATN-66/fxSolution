using System.Reflection;
using System.Text;
using Newtonsoft.Json;
using Terminal.WinUI3.Contracts.Services;
using Windows.Storage;

namespace Terminal.WinUI3.Services;

public class FileService : IFileService
{
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
