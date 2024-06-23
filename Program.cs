using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using BenchmarkDotNet.Running;
using static System.Console;
using Wave.LogChallenge;
using Wave.LogChallenge.Implementations;

//BenchmarkRunner.Run<LogBenchmarks>(null, args);
//return;

try
{
    using var source = new CancellationTokenSource();
    TreatControlCAsInput = false;
    CancelKeyPress += [SuppressMessage("ReSharper", "AccessToDisposedClosure")](_, e) =>
    {
        source.Cancel();
        e.Cancel = true;
    };
    const string fileName = "itcont.txt";
    await RunAsync(new CharLogReader(fileName), source.Token);
    await RunAsync(new FastCharLogReader(fileName), source.Token);
    return;
    await RunAsync(new ByteLogReader(fileName), source.Token);
    await RunAsync(new FastLogReader(fileName), source.Token);
    await RunAsync(new SuperFastLogReader(fileName), source.Token);
    await RunAsync(new SyncSuperFastLogReader(fileName), source.Token);
    await WaitForKeyAsync(source.Token);
}
catch (OperationCanceledException)
{ }
return;

static async Task RunAsync(IAsyncLogReader logReader, CancellationToken token = default)
{
    ArgumentNullException.ThrowIfNull(logReader);
    WriteLine($"--- {logReader.GetType().Name} ---");
    var watch = new Stopwatch();
    var progress = new Progress<decimal>();
    progress.ProgressChanged += OnProgressOnProgressChanged;
    watch.Start();
    (long count, string firstName, string secondName, string eachMonth, string commonName) = await logReader.ReadAsync(progress, token);
    watch.Stop();
    progress.ProgressChanged -= OnProgressOnProgressChanged;
    if (token.IsCancellationRequested) return;
    await Task.Delay(TimeSpan.FromSeconds(1), token);
    WriteLine();
    WriteLine(count);
    WriteLine(firstName);
    WriteLine(secondName);
    WriteLine(eachMonth);
    WriteLine(commonName);
    WriteLine(watch.Elapsed);
    return;

    void OnProgressOnProgressChanged(object? _, decimal e)
    {
        ForegroundColor = e switch
        {
            < .0m => ConsoleColor.White,
            < .1m => ConsoleColor.DarkCyan,
            < .2m => ConsoleColor.Cyan,
            < .3m => ConsoleColor.DarkBlue,
            < .4m => ConsoleColor.Blue,
            < .5m => ConsoleColor.DarkYellow,
            < .6m => ConsoleColor.Yellow,
            < .7m => ConsoleColor.DarkMagenta,
            < .8m => ConsoleColor.Magenta,
            < .9m => ConsoleColor.DarkRed,
            _ => ConsoleColor.Red
        };
        Write($"\r{e,6:P1} ({watch.Elapsed:hh\\:mm\\:ss\\.ff})");
        ResetColor();
    }
}


static async Task WaitForKeyAsync(CancellationToken token = default)
{
    await Task.Yield();
#if DEBUG
    while (! token.IsCancellationRequested && ! KeyAvailable)
    {
        await Task.Delay(TimeSpan.FromMilliseconds(1.0), token);
    }
    ReadKey(true);
#endif
}
