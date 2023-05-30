using System.Text.Json;
using Terminal.WinUI3.Contracts.Services;
using Terminal.WinUI3.Models.Dashboard;

namespace Terminal.WinUI3.Services;

public class DashboardService : IDashboardService
{
    private readonly IFileService _fileService;

    public IList<DashboardGroup> Groups
    {
        get;
    } = new List<DashboardGroup>();

    public DashboardService(IFileService fileService)
    {
        _fileService = fileService;
    }

    public async Task InitializeAsync()
    {
        var jsonText = await _fileService.LoadTextAsync("Dashboard.json").ConfigureAwait(false);
        var controlInfoDataGroup = JsonSerializer.Deserialize<Root>(jsonText, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        string pageRoot = "AppUIBasics.ControlPages.";

        //controlInfoDataGroup.Groups.SelectMany(g => g.Items).ToList().ForEach(item =>
        //{
        //    string? badgeString = item switch
        //    {
        //        { IsNew: true } => "New",
        //        { IsUpdated: true } => "Updated",
        //        { IsPreview: true } => "Preview",
        //        _ => null
        //    };
        //    string pageString = $"{pageRoot}{item.UniqueId}Page";

        //    Type? pageType = Type.GetType(pageString);

        //    item.BadgeString = badgeString;
        //    item.IncludedInBuild = pageType is not null;
        //});

        //foreach (var group in controlInfoDataGroup.Groups)
        //{
        //    if (!Groups.Any(g => g.Title == group.Title))
        //    {
        //        Groups.Add(group);
        //    }
        //}

    }

    public async Task<IEnumerable<DashboardGroup>> GetGroupsAsync()
    {
        throw new NotImplementedException();
        //await _instance.GetControlInfoDataAsync();

        //return _instance.Groups;
    }

    public async Task<DashboardGroup> GetGroupAsync(string uniqueId)
    {
        throw new NotImplementedException();
        //await _instance.GetControlInfoDataAsync();
        //var matches = _instance.Groups.Where((group) => group.UniqueId.Equals(uniqueId));
        //if (matches.Count() == 1) return matches.First();
        //return null;
    }

    public async Task<DashboardItem> GetItemAsync(string uniqueId)
    {
        throw new NotImplementedException();
        //await _instance.GetControlInfoDataAsync();
        //// Simple linear search is acceptable for small data sets
        //var matches = _instance.Groups.SelectMany(group => group.Items).Where((item) => item.UniqueId.Equals(uniqueId));
        //if (matches.Count() > 0) return matches.First();
        //return null;
    }

    public async Task<DashboardGroup> GetGroupFromItemAsync(string uniqueId)
    {
        throw new NotImplementedException();
        //await _instance.GetControlInfoDataAsync();
        //var matches = _instance.Groups.Where((group) => group.Items.FirstOrDefault(item => item.UniqueId.Equals(uniqueId)) != null);
        //if (matches.Count() == 1) return matches.First();
        //return null;
    }
}