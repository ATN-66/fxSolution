using Common.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Reflection;
using System.Globalization;

namespace Common.DataSource;

public abstract class DataSource : IDataSource
{
    private readonly Guid _guid = Guid.NewGuid();
    protected readonly ILogger<IDataSource> _logger;
    private readonly IAudioPlayer _audioPlayer;

    private readonly ConcurrentDictionary<DateTime, List<Quotation>> _hoursCache = new();
    private readonly ConcurrentQueue<DateTime> _hoursKeys = new();
    private DateTime _currentHoursKey;
    private readonly int _maxHoursInCache;

    protected DataSource(IConfiguration configuration, ILogger<IDataSource> logger, IAudioPlayer audioPlayer)
    {
        _logger = logger;
        _audioPlayer = audioPlayer;

        _currentHoursKey = DateTime.Now.Date.AddHours(DateTime.Now.Hour);
        _maxHoursInCache = configuration.GetValue<int>($"{nameof(_maxHoursInCache)}");
        _logger.LogTrace("({Guid}) is ON.", _guid);
    }

    public async Task<IList<Quotation>> GetHistoricalDataAsync(DateTime startDateTimeInclusive, DateTime endDateTimeInclusive, CancellationToken token)
    {
        var difference = Math.Ceiling((endDateTimeInclusive - startDateTimeInclusive).TotalHours);
        if (difference > _maxHoursInCache)
        {
            throw new InvalidOperationException($"Requested hours exceed maximum cache size of {_maxHoursInCache} hours.");
        }
        if (difference < 0)
        {
            throw new InvalidOperationException("Start date cannot be later than end date.");
        }

        var tasks = new List<Task>();
        var quotations = new List<Quotation>();
        var start = startDateTimeInclusive.Date.AddHours(startDateTimeInclusive.Hour);
        var end = endDateTimeInclusive.Date.AddHours(endDateTimeInclusive.Hour).AddHours(1);

        if (_currentHoursKey != DateTime.Now.Date.AddHours(DateTime.Now.Hour))
        {
            while (!_hoursKeys.IsEmpty)
            {
                if (_hoursKeys.TryDequeue(out var oldKey))
                {
                    _hoursCache.TryRemove(oldKey, out _);
                }
            }

            if (!_hoursKeys.IsEmpty || !_hoursCache.IsEmpty)
            {
                throw new Exception("!_hoursKeys.IsEmpty || !_hoursCache.IsEmpty");
            }
        }

        _currentHoursKey = DateTime.Now.Date.AddHours(DateTime.Now.Hour);

        var key = start;
        do
        {
            if (!_hoursCache.ContainsKey(key) || _currentHoursKey.Equals(key))
            {
                tasks.Add(LoadHistoricalDataAsync(key, key, token));
            }

            key = key.Add(new TimeSpan(1, 0, 0));

        }
        while (key < end);

        await Task.WhenAll(tasks).ConfigureAwait(false);

        key = start;
        do
        {
            if (_hoursCache.ContainsKey(key))
            {
                quotations.AddRange(GetData(key));
            }
            else
            {
                throw new InvalidOperationException("The key is absent.");
            }

            key = key.Add(new TimeSpan(1, 0, 0));
        }
        while (key < end);

        return quotations;
    }
    private async Task LoadHistoricalDataAsync(DateTime startDateTimeInclusive, DateTime endDateTimeInclusive, CancellationToken token)
    {
        if (!startDateTimeInclusive.Equals(endDateTimeInclusive))
        {
            throw new InvalidOperationException("Start and end times must be the same. This function only supports processing of a single hour at a time.");
        }

        try
        {
            var quotations = await GetDataAsync(startDateTimeInclusive, token).ConfigureAwait(false);
            ProcessData(endDateTimeInclusive, quotations);
        }
        catch (Exception exception)
        {
            LogException(exception, "");
            throw;
        }
    }
    protected abstract Task<IList<Quotation>> GetDataAsync(DateTime startDateTimeInclusive, CancellationToken token);
    private void ProcessData(DateTime dateTime, IEnumerable<Quotation> quotations)
    {
        var done = false;
        var groupedByYear = quotations.GroupBy(q => new QuotationKey { Year = q.StartDateTime.Year });
        foreach (var yearGroup in groupedByYear)
        {
            var year = yearGroup.Key.Year;
            var groupedByMonth = yearGroup.GroupBy(q => new QuotationKey { Month = q.StartDateTime.Month });
            foreach (var monthGroup in groupedByMonth)
            {
                var month = monthGroup.Key.Month;
                var groupedByDay = monthGroup.GroupBy(q => new QuotationKey { Day = q.StartDateTime.Day });
                foreach (var dayGroup in groupedByDay)
                {
                    var day = dayGroup.Key.Day;
                    var groupedByHour = dayGroup.GroupBy(q => new QuotationKey { Hour = q.StartDateTime.Hour });
                    foreach (var hourGroup in groupedByHour)
                    {
                        var hour = hourGroup.Key.Hour;
                        var key = new DateTime(year, month, day, hour, 0, 0);
                        var quotationsToSave = hourGroup.ToList();
                        SetData(key, quotationsToSave);
                        done = true;
                        _logger.LogTrace("year:{year}, month:{month:D2}, day:{day:D2}, hour:{hour:D2}. Count:{quotationsToSaveCount}", year.ToString(), month.ToString(), day.ToString(), hour.ToString(), quotationsToSave.Count.ToString());
                    }
                }
            }
        }

        if (!done)
        {
            SetData(dateTime, quotations: new List<Quotation>());
        }
    }
    private void AddData(DateTime key, List<Quotation> quotations)
    {
        if (_hoursCache.Count >= _maxHoursInCache)
        {
            if (_hoursKeys.TryDequeue(out var oldestKey))
            {
                _hoursCache.TryRemove(oldestKey, out _);
            }
        }

        if (!_hoursCache.ContainsKey(key))
        {
            _hoursCache[key] = quotations;
            _hoursKeys.Enqueue(key);
        }
        else
        {
            if (!_currentHoursKey.Equals(key))
            {
                throw new InvalidOperationException("The key already exists.");
            }

            _hoursCache[key] = quotations;
        }
    }
    private void SetData(DateTime key, List<Quotation> quotations)
    {
        AddData(key, quotations);
    }
    private IEnumerable<Quotation> GetData(DateTime key)
    {
        _hoursCache.TryGetValue(key, out var quotations);
        return quotations!;
    }
    protected static int Week(DateTime dateTime)
    {
        var weekNumber = CultureInfo.InvariantCulture.Calendar.GetWeekOfYear(dateTime, CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday);
        return weekNumber;
    }
    protected static int Quarter(int weekNumber)
    {
        const string errorMessage = $"{nameof(weekNumber)} is out of range.";
        return weekNumber switch
        {
            <= 0 => throw new Exception(errorMessage),
            <= 13 => 1,
            <= 26 => 2,
            <= 39 => 3,
            <= 52 => 4,
            _ => throw new Exception(errorMessage)
        };
    }

    protected void LogException(Exception exception, string? message = null)
    {
        _audioPlayer.Play();

        // Log optional message
        if (!string.IsNullOrEmpty(message))
        {
            _logger.LogError("Custom Message: {Message}", message);
        }

        // Log exception details
        _logger.LogError("Exception Details: ");

        // Use loop to log inner exceptions
        var currentException = exception;
        var exceptionLevel = 1;
        while (currentException != null)
        {
            _logger.LogError("Exception (level {Level}): {exceptionMessage}", exceptionLevel++, currentException.Message);
            currentException = currentException.InnerException;
        }

        // Log stack trace
        _logger.LogError("<-------- StackTrace -------->");
        _logger.LogError("{ExceptionType}: {exceptionMessage}\n{StackTrace}", exception.GetType(), exception.Message, exception.StackTrace);
    }
}