/*+------------------------------------------------------------------+
  |                                    Terminal.WinUI3.Models.Kernels|
  |                                              DataSourceKernel.cs |
  +------------------------------------------------------------------+*/

using Common.Entities;
using Terminal.WinUI3.Contracts.Models;
using Terminal.WinUI3.Contracts.Services;
using Terminal.WinUI3.Models.Entities;
using Quotation = Common.Entities.Quotation;

namespace Terminal.WinUI3.Models.Kernels;

public abstract class DataSourceKernel<TItem> : IDataSourceKernel<TItem> where TItem : IChartItem
{
    private readonly IFileService _fileService;
    private const string FolderPath = @"D:\forex.Terminal.WinUI3.Logs\";//todo: move to config

    protected readonly List<TItem> Items = new();

    protected DataSourceKernel(IFileService fileService)
    {
        _fileService = fileService;
    }

    public int Count => Items.Count;
    public TItem this[int i]
    {
        get
        {
            if (i < 0 || i >= Items.Count)
            {
                throw new IndexOutOfRangeException($"Index {i} is out of range. There are only {Items.Count} items.");
            }

            return Items[Items.Count - 1 - i];
        }
    }
    
    public abstract void AddRange(IEnumerable<Quotation> quotations);
    public abstract void Add(Quotation quotation);
    public abstract int FindIndex(DateTime dateTime);
    public abstract TItem? FindItem(DateTime dateTime);
    public abstract void SaveItems((DateTime first, DateTime second) dateRange);
    protected void SaveItemsToJson(IEnumerable<IChartItem> items, Symbol symbol, string typeName)
    {
        var symbolName = symbol.ToString();
        var fullFileName = $"{symbolName}_{typeName}_tbars.json";
        _fileService.Save(FolderPath, fullFileName, items);
    }
}