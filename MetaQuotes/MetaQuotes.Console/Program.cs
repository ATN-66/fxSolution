/*+------------------------------------------------------------------+
  |                             MetaQuotes.Simulator.PipeMethodCalls |
  |                                                       Program.cs |
  +------------------------------------------------------------------+*/

using Common.Entities;
using MetaQuotes.Console;
using MetaQuotes.Data;
using NAudio.Wave;

var config = new Configuration()
{
    Workplace = Workplace.Development,
    Year = 2023,
    Week = 8,
    Day = 0 // 7 or 0
};

const string mt5Format = "yyyy.MM.dd HH:mm:ss"; // 2023.05.08 19:52:22 <- from MT5 
const string ok = "ok";
const string audioFilePath = "alert2.wav";
var audioPlayer = new AudioPlayer(audioFilePath);

Console.WriteLine("MetaQuotes.MT5 platform simulator...");
Console.WriteLine($"Workplace: {config.Workplace}.");
Console.WriteLine($"Year: {config.Year}.");
Console.WriteLine($"Week: {config.Week?.ToString("00") ?? "null"}.");
Console.WriteLine($"Day: {config.Day?.ToString("00") ?? "null"}.");

var (firstQuotations, quotations) = await MSSQLRepository.Instance.GetQuotationsForDayAsync(config.Year, config.Week!.Value, config.Day!.Value).ConfigureAwait(false);

CancellationTokenSource cts = new();
var consoleServiceTask = Task.Run(() => ConsoleService(cts));

try
{
    await InitializeIndicators(firstQuotations, config.Workplace, cts.Token).ConfigureAwait(false);
    Console.WriteLine("Initialization done...");
    await ProcessQuotations(quotations, cts.Token).ConfigureAwait(false);
    Console.WriteLine("Quotations done...");
    await DeInitializeIndicators().ConfigureAwait(false);
    Console.WriteLine("DeInitialization done...");
    cts.Cancel();
}
catch (Exception exception)
{
    EmergencyExit(exception);
    return -1;
}

await Task.WhenAny(consoleServiceTask).ConfigureAwait(false);

Console.WriteLine("End of the program. Press any key to exit ...");
Console.ReadKey();
return 1;

static Task DeInitializeIndicators()
{
    foreach (var symbol in Enum.GetValues(typeof(Symbol)))
    {
        DataMediator.DeInit((int)symbol, (int)DeInitReason.Terminal_closed);
    }

    return Task.CompletedTask;
}

static Task InitializeIndicators(Queue<Quotation> firstQuotations, Workplace space, CancellationToken ct)
{
    while (firstQuotations.Count > 0)
    {
        if (ct.IsCancellationRequested) break;
        var quotation = firstQuotations.Dequeue();
        var output = DataMediator.Init((int)quotation.Symbol, quotation.Start.ToString(mt5Format), quotation.Ask, quotation.Bid, (int)space).Split(':');
        var symbol = (Symbol)Convert.ToInt32(output[0]);
        var guid = Guid.Parse(output[1]);
        var result = output[2];
        Console.WriteLine($"Init result: {symbol}({guid}): {result}");
        if (ok != result) throw new Exception(result);
    }
    return Task.CompletedTask;
}

static Task ProcessQuotations(Queue<Quotation> quotations, CancellationToken ct)
{
    while (quotations.Count > 0)
    {
        if (ct.IsCancellationRequested) break;
        var quotation = quotations.Dequeue();
        //await Task.Delay(100, ct).ConfigureAwait(false);
        var result = DataMediator.Tick((int)quotation.Symbol, quotation.Start.ToString(mt5Format), quotation.Ask, quotation.Bid);
        if (ok != result) throw new Exception(result);
    }

    return Task.CompletedTask;
}

static async Task ConsoleService(CancellationTokenSource cts)
{
    while (!cts.Token.IsCancellationRequested)
        if (Console.KeyAvailable)
        {
            var input = Console.ReadLine();

            if (input == null)
            {
                await Task.Delay(100, cts.Token).ConfigureAwait(false);
                continue;
            }

            switch (input.ToLower())
            {
                case "exit":
                    cts.Cancel();
                    return;
                case "clear":
                    Console.Clear();
                    break;
                default:
                    Console.WriteLine($"Unknown command: {input}");
                    break;
            }
        }
        else
        {
            await Task.Delay(100, cts.Token).ConfigureAwait(false);
        }
}

void EmergencyExit(Exception exception)
{
    audioPlayer.Play();
    //Debug.WriteLine(exception.Message);
    Console.WriteLine(exception.Message);
    Console.WriteLine("Server is OFF. Press any key to exit...");
    Console.ReadKey();
}

internal readonly struct Configuration
{
    public Workplace Workplace { get; init; }
    public int Year { get; init; }
    public int? Week { get; init; }
    public int? Day { get; init; }
}

internal class AudioPlayer : IDisposable
{
    private readonly AudioFileReader _audioFileReader;
    private readonly WaveOutEvent _waveOut;

    public AudioPlayer(string audioFilePath)
    {
        _audioFileReader = new AudioFileReader(audioFilePath);
        _waveOut = new WaveOutEvent();
        _waveOut.Init(_audioFileReader);
    }

    public void Play()
    {
        _waveOut.Play();
    }

    public void Dispose()
    {
        _waveOut.Dispose();
        _audioFileReader.Dispose();
    }
}


