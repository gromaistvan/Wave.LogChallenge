using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using static System.Console;
using Wave.LogChallenge;
using Wave.LogChallenge.Implementations;

try
{
    using var source = new CancellationTokenSource();
    TreatControlCAsInput = false;
    CancelKeyPress += [SuppressMessage("ReSharper", "AccessToDisposedClosure")](_, e) =>
    {
        source.Cancel();
        e.Cancel = true;
    };
    await RunAsync(new CharLogReader("itcont.txt"), source.Token);
    await RunAsync(new ByteLogReader("itcont.txt"), source.Token);
    await RunAsync(new FastLogReader("itcont.txt"), source.Token);
    await RunAsync(new SuperFastLogReader("itcont.txt"), source.Token);
    await RunAsync(new SyncSuperFastLogReader("itcont.txt"), source.Token);
#if DEBUG
    await WaitForKeyAsync(source.Token);
#endif
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

#if DEBUG
static async Task WaitForKeyAsync(CancellationToken token = default)
{
    while (! token.IsCancellationRequested && ! KeyAvailable)
    {
        await Task.Delay(TimeSpan.FromMilliseconds(1.0), token);
    }
    ReadKey(true);
}
#endif