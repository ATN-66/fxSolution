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
    }

    public string? SelectedItem
    {
        get;
        set;
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

    public ObservableCollection<TitledGroups> GetTitledGroups()
    {
        var query = from g in Groups 
            select new TitledGroups(g.Items)
            {
                Key = g.Id,
                Title = g.Title
            };

        var result = new ObservableCollection<TitledGroups>(query);

        if (SelectedItem is null)
        {
            return result;
        }

        foreach (var group in result)
        {
            foreach (var item in group)
            {
                item.IsSelected = item.Id == SelectedItem;
            }
        }

        return result;
    }
}