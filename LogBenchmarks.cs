using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Jobs;
using Wave.LogChallenge.Implementations;

namespace Wave.LogChallenge;

[DryJob(RuntimeMoniker.Net80, Jit.RyuJit, Platform.Arm64)]
[MemoryDiagnoser]
[PlainExporter]
public class LogBenchmarks
{
    private const string FileName = "itcont.txt";
    
    [Benchmark] public async Task ByteLogReader() => await new ByteLogReader(FileName).ReadAsync(); 
    [Benchmark] public async Task FastLogReader() => await new FastLogReader(FileName).ReadAsync(); 
    [Benchmark] public async Task SuperFastLogReader() => await new SuperFastLogReader(FileName).ReadAsync();
    [Benchmark] public async Task SyncSuperFastLogReader() => await new SyncSuperFastLogReader(FileName).ReadAsync();
    [Benchmark] public async Task FastCharLogReader() => await new FastCharLogReader(FileName).ReadAsync();
    [Benchmark] public async Task CharLogReader() => await new CharLogReader(FileName).ReadAsync(); 
}