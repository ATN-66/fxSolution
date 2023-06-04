using System.Collections.ObjectModel;
using System.Text.Json;
using Terminal.WinUI3.Contracts.Services;
using Terminal.WinUI3.Models.Dashboard;

namespace Terminal.WinUI3.Services;

public class DashboardService : IDashboardService
{
    private readonly IFileService _fileService;

    private IList<DashboardGroup> Groups
    {
        get;
    } = new List<DashboardGroup>();

    public DashboardService(IFileService fileService)
    {
        _fileService = fileService;
        SelectedDashboardItemId = "Backup";//todo:
    }

    public async Task InitializeAsync()
    {
        var jsonText = await _fileService.LoadTextAsync("Dashboard.json").ConfigureAwait(false);
        var input = JsonSerializer.Deserialize<Root>(jsonText, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        foreach (var group in input!.Groups!)
        {
            if (Groups.All(g => g.Title != group.Title))
            {
                Groups.Add(group);
            }
        }
    }

    public ObservableCollection<GroupTitleList> GetGroupsWithItems()
    {
        var query = from g in Groups 
            select new GroupTitleList(g.Items)
            {
                Key = g.Id,
                Title = g.Title
            };

        return new ObservableCollection<GroupTitleList>(query);
    }

    public string SelectedDashboardItemId
    {
        get;
        set;
    }
}