using System.Diagnostics;
using System.Threading.Tasks;
using static System.Console;
using Wave.LogChallenge;

await RunAsync(new LogReader("itcont.txt"));
await RunAsync(new LogReaderAlt("itcont.txt"));
await RunAsync(new FastLogReader("itcont.txt"));
return;

static async Task RunAsync(ILogReader logReader)
{
    var watch = Stopwatch.StartNew();
    (long count, string firstName, string secoundName, string eachMonth, string commonName) = await logReader.ReadAsync();
    watch.Stop();
    WriteLine(count);
    WriteLine(firstName);
    WriteLine(secoundName);
    WriteLine(eachMonth);
    WriteLine(commonName);
    WriteLine(watch.Elapsed);
}