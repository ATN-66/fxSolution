using System.Text;
using Mediator.Contracts.Services;
using Newtonsoft.Json;

namespace Mediator.Services;

public class FileService : IFileService
{
    public T Read<T>(string folderPath, string fileName)
    {
        var path = Path.Combine(folderPath, fileName);
        if (!File.Exists(path))
        {
            return default!;
        }

        var json = File.ReadAllText(path);
        return JsonConvert.DeserializeObject<T>(json)!;
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
        catch (Exception ex) {}
        {

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
