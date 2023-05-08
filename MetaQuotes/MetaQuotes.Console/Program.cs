/*+------------------------------------------------------------------+
  |                             MetaQuotes.Simulator.PipeMethodCalls |
  |                                                       Program.cs |
  +------------------------------------------------------------------+*/

using System.Diagnostics;
using Common.Entities;
using MetaQuotes.Client.IndicatorToMediator;
using MetaQuotes.Console;
using NAudio.Wave;
using Environment = Common.Entities.Environment;

const Environment environment = Environment.Testing;
const Tick inputTick = Tick.UnModified;
const int year = 2023;
int? week = 8;
int? day = 3;

const string ok = "ok";
const string audioFilePath = "alert2.wav";
var audioFileReader = new AudioFileReader(audioFilePath);
var waveOut = new WaveOutEvent();
waveOut.Init(audioFileReader);

Console.WriteLine("MT5 platform...");
Console.WriteLine($"Environment: {environment}.");
Console.WriteLine($"Input Ticks: {inputTick}.");
Console.WriteLine($"Year: {year}.");
Console.WriteLine($"Week: {week?.ToString("00") ?? "null"}.");
Console.WriteLine($"Day: {day?.ToString("00") ?? "null"}.");

CancellationTokenSource cts = new();
var consoleServiceTask = Task.Run(() => HandleConsole(cts));

var (firstQuotations, quotations) = await MSSQLRepository.Instance.GetQuotationsForDayAsync(year, week!.Value, day!.Value, environment, inputTick).ConfigureAwait(false);
try
{
    while (firstQuotations.Count > 0)
    {
        var q = firstQuotations.Dequeue();
        var result = IndicatorToMediatorService.Init((int)q.Symbol, q.DateTime.ToString("yyyy.MM.dd HH:mm:ss"), q.Ask, q.Bid, (int)environment);
        if (ok != result) throw new Exception(result);
    }
}
catch (Exception exception)
{
    EmergencyExit(exception);
    return -1;
}

var OnTickTask = Task.Run(async () =>
{
    try
    {
        while (quotations.Count > 0)
        {
            if (cts.Token.IsCancellationRequested) break;
            var quotation = quotations.Dequeue();
            var result = IndicatorToMediatorService.Tick((int)quotation.Symbol, quotation.DateTime.ToString("yyyy.MM.dd HH:mm:ss"), quotation.Ask, quotation.Bid);
            if (ok != result) throw new Exception(result);
            await Task.Delay(10, cts.Token).ConfigureAwait(false); //TODO: adjust delay
        }
        foreach (var symbol in Enum.GetValues(typeof(Symbol))) IndicatorToMediatorService.DeInit((int)symbol, (int)DeInitReason.Terminal_closed);
    }
    catch (Exception exception)
    {
        EmergencyExit(exception);
        return -1;
    }

    Console.WriteLine("There is no more quotations.");
    return 0;
}, cts.Token);

await Task.WhenAny(consoleServiceTask, OnTickTask).ConfigureAwait(false);

Console.WriteLine("End of the program. Press any key to exit ...");
Console.ReadKey();
return 1;

static async Task HandleConsole(CancellationTokenSource cts)
{
    while (!cts.Token.IsCancellationRequested)
        if (Console.KeyAvailable)
        {
            var input = Console.ReadLine();

            if (input == null)
            {
                await Task.Delay(100).ConfigureAwait(false);
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
            await Task.Delay(100).ConfigureAwait(false);
        }
}

void EmergencyExit(Exception exception)
{
    waveOut.Play();
    Debug.WriteLine(exception.Message);
    Console.WriteLine(exception.Message);
    Console.WriteLine("Server is OFF. Press any key to exit...");
    Console.ReadKey();
}