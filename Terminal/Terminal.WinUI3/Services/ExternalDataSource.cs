/*+------------------------------------------------------------------+
  |                                          Terminal.WinUI3.Services|
  |                                            ExternalDataSource.cs |
  +------------------------------------------------------------------+*/

using Common.Entities;
using Terminal.WinUI3.Contracts.Services;

namespace Terminal.WinUI3.Services;

public class ExternalDataSource : IExternalDataSource
{
    private readonly IFileService _fileService;
    private readonly IMediator _mediator;

    public ExternalDataSource(IFileService fileService, IMediator mediator)
    {
        _fileService = fileService;
        _mediator = mediator;
    }

    public async Task<IEnumerable<Quotation>> GetTicksAsync(DateTime startDateTime, DateTime endDateTime, Provider provider, bool exactly)
    {
        IEnumerable<Quotation> input;
        List<Quotation> result;

        if (exactly)
        {
            switch (provider)
            {
                case Provider.Mediator:
                    input = await _mediator.GetTicksAsync(startDateTime, endDateTime).ConfigureAwait(false);
                    result = input.ToList();
                    break;
                case Provider.FileService:
                    input = await _fileService.GetTicksAsync(startDateTime, endDateTime).ConfigureAwait(false);
                    result = input.ToList();
                    break;
                case Provider.Terminal:
                default: throw new ArgumentOutOfRangeException(nameof(provider), provider, null);
            }
        }
        else
        {
            switch (provider)
            {
                case Provider.Mediator:
                    input = await _mediator.GetTicksAsync(startDateTime, endDateTime).ConfigureAwait(false);
                    result = input.ToList();
                    if (result.Count == 0)
                    {
                        input = await _fileService.GetTicksAsync(startDateTime, endDateTime).ConfigureAwait(false);
                        result = input.ToList();
                    }
                    break;
                case Provider.FileService:
                    input = await _fileService.GetTicksAsync(startDateTime, endDateTime).ConfigureAwait(false);
                    result = input.ToList();
                    if (result.Count == 0)
                    {
                        input = await _mediator.GetTicksAsync(startDateTime, endDateTime).ConfigureAwait(false);
                        result = input.ToList();
                    }
                    break;
                case Provider.Terminal:
                default: throw new ArgumentOutOfRangeException(nameof(provider), provider, null);
            }
        }

        return result;
    }
}