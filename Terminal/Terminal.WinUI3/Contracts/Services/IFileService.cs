using Common.Entities;

namespace Terminal.WinUI3.Contracts.Services;

public interface IFileService
{
    Task<IEnumerable<Quotation>> GetTicksAsync(DateTime startDateTimeInclusive, DateTime endDateTimeInclusive);

    T? Read<T>(string folderPath, string fileName);
    void Save<T>(string folderPath, string fileName, T content);
    void Delete(string folderPath, string? fileName);
    Task<string> LoadTextAsync(string relativeFilePath);
}