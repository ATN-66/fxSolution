/*+------------------------------------------------------------------+
  |                                          Terminal.WinUI3.Services|
  |                                            ExternalDataSource.cs |
  +------------------------------------------------------------------+*/

using Common.Entities;
using Terminal.WinUI3.Contracts.Services;

namespace Terminal.WinUI3.Services;

//TODO HOURLY
public class ExternalDataSource : IExternalDataSource
{
    private readonly IFileService _fileService;
    private readonly IMediator _mediator;

    public ExternalDataSource(IFileService fileService, IMediator mediator)
    {
        _fileService = fileService;
        _mediator = mediator;
    }

    public async Task<IEnumerable<Quotation>> GetTicksAsync(DateTime startDateTimeInclusive, DateTime endDateTimeInclusive, Provider provider, bool exactly)
    {
        if (exactly)
        {
            switch (provider)
            {
                case Provider.Mediator:
                    var inputMediator = await _mediator.GetHistoricalDataAsync(startDateTimeInclusive, endDateTimeInclusive).ConfigureAwait(false);
                    var resultMediator = inputMediator.ToList();
                    return resultMediator;
                case Provider.FileService:
                    var inputFileService = await _fileService.GetTicksAsync(startDateTimeInclusive, endDateTimeInclusive).ConfigureAwait(false);
                    var resultFileService = inputFileService.ToList();
                    return resultFileService;
                case Provider.Terminal:
                default: throw new ArgumentOutOfRangeException(nameof(provider), provider, @"Mediator or FileService Providers accepted.");
            }
        }
        else
        {
            throw new NotImplementedException();

            //var input_mediator = await _mediator.GetHistoricalDataAsync(startDateTimeInclusive, endDateTimeInclusive).ConfigureAwait(false);
            //var result_mediator = input_mediator.ToList();
            //var input_fileService = await _fileService.GetHistoricalDataAsync(startDateTimeInclusive, endDateTimeInclusive).ConfigureAwait(false);
            //var result_fileService = input_fileService.ToList();


            //sample var fileServiceTask = _dataService.GetHistoricalDataAsync(SelectedSymbol, dateTimeInclusive, endDateTimeInclusive, Provider.FileService, true);
            //await Task.WhenAll(fileServiceTask, mediatorTask, terminalTask).ConfigureAwait(true);





            //switch (provider)
            //{
            //    //case Provider.Mediator:

            //    //    if (result.Count == 0)
            //    //    {
            //    //        input = await _fileService.GetHistoricalDataAsync(startDateTimeInclusive, endDateTimeInclusive).ConfigureAwait(false);
            //    //        result = input.ToList();
            //    //    }
            //    //    break;
            //    //case Provider.FileService:
            //    //    input = await _fileService.GetHistoricalDataAsync(startDateTimeInclusive, endDateTimeInclusive).ConfigureAwait(false);
            //    //    result = input.ToList();
            //    //    if (result.Count == 0)
            //    //    {
            //    //        input = await _mediator.GetHistoricalDataAsync(startDateTimeInclusive, endDateTimeInclusive).ConfigureAwait(false);
            //    //        result = input.ToList();
            //    //    }
            //    //    break;
            //    //case Provider.Terminal:
            //    //default: throw new ArgumentOutOfRangeException(nameof(provider), provider, null);
            //}
        }
    }
}