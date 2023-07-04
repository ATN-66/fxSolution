using System.Collections.ObjectModel;
using System.Reflection;
using System.Text.Json;
using Common.ExtensionsAndHelpers;
using Microsoft.Extensions.Logging;
using Terminal.WinUI3.Contracts.Services;
using Terminal.WinUI3.Models.Dashboard;

namespace Terminal.WinUI3.Services;

public class DashboardService : IDashboardService
{
    private readonly IFileService _fileService;
    private readonly ILogger<IDashboardService> _logger;

    private IList<DashboardGroup> DashboardGroups
    {
        get;
    } = new List<DashboardGroup>();

    public DashboardService(IFileService fileService, ILogger<IDashboardService> logger)
    {
        _fileService = fileService;
        _logger = logger;
    }

    public string? SelectedItem
    {
        get;
        set;
    }

    public async Task InitializeAsync()
    {
        try
        {
            var jsonText = await _fileService.LoadTextAsync("dashboard.json").ConfigureAwait(false);
            var input = JsonSerializer.Deserialize<Root>(jsonText, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            foreach (var group in input!.DashboardGroups!)
            {
                if (DashboardGroups.All(g => g.Title != group.Title))
                {
                    DashboardGroups.Add(group);
                }
            }
        }
        catch (Exception exception)
        {
            LogExceptionHelper.LogException(_logger, exception, "");
            throw;
        }
    }

    public ObservableCollection<TitledGroups> GetTitledGroups()
    {
        var query = from g in DashboardGroups 
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