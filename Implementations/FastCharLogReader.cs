using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using static System.IO.FileMode;
using static System.IO.FileOptions;

namespace Wave.LogChallenge.Implementations;

internal class FastCharLogReader(string fileName): IAsyncLogReader
{
    private const int ReportCnt = 500_000, ThreadCnt = 100;
    
    private sealed class State
    {
        private readonly ConcurrentDictionary<string, long> _names = new(concurrencyLevel: ThreadCnt, capacity: 100_000, comparer: StringComparer.Ordinal);
        
        private readonly long[] _months = new long[12];

        private long _lines, _maxNameOccurrence = 1L;
        
        private string _firstName = "", _secondName = "", _commonName = "";

        private void ProcessLineCore(ReadOnlySpan<char> line, long count)
        {
            const char separator = '|', zero = '0';
            int column = 0, nameStart = 0;
            for (var i = 0; i < line.Length; ++i)
            {
                if (line[i] != separator) continue;
                switch (++column)
                {
                    case 4:
                        ref long pos = ref _months[(line[i + 5] - zero) * 10 + (line[i + 6] - zero) - 1];
                        Interlocked.Increment(ref pos);
                        i += 19;
                        ++column;
                        break;
                    case 7:
                        nameStart = i + 1;
                        break;
                    case 8:
                        var name = line[nameStart..i].ToString();
                        switch (count)
                        {
                            case 432L:   _firstName = name;  break;
                            case 43243L: _secondName = name; break;
                        }
                        _names.AddOrUpdate(
                            name, 
                            _ => 1L, 
                            (n, v) =>
                            {
                                if (v < _maxNameOccurrence) return v + 1;
                                _commonName = n;
                                return Interlocked.Increment(ref _maxNameOccurrence);
                            });
                        return;
                }
            }
            throw new ArgumentException("Not enough columns!", nameof(line));
        }

        public void ProcessLine(string line) =>
            ProcessLineCore(line, ++_lines);

        public Task ProcessLineAsync(string line, CancellationToken token = default) =>
            Task.Run(() => ProcessLineCore(line, Interlocked.Increment(ref _lines)), token);

        public void Deconstruct(out long lines, out string firstName, out string secondName, out string eachMonth, out string commonName) =>
            (lines, firstName, secondName, eachMonth, commonName) = (_lines, _firstName, _secondName, string.Join(',', _months), _commonName);
    }
    
    private readonly long _size = new FileInfo(fileName).Length;
    
    public (long lines, string firstName, string secondName, string eachMonth, string commonName) Read(IProgress<decimal>? progress = null)
    {
        using StreamReader file = new(
            fileName,
            Encoding.ASCII,
            detectEncodingFromByteOrderMarks: false,
            new FileStreamOptions { Mode = Open, Access = FileAccess.Read, Share = FileShare.Read, Options = SequentialScan });
        State state = new();
        progress?.Report(0m);
        (var pos, int cnt) = (0m, ReportCnt);
        while (file.ReadLine() is { } line)
        {
            state.ProcessLine(line);
            pos += line.Length + 1;
            if (--cnt >= 0) continue;
            progress?.Report(pos / _size);
            cnt = ReportCnt;
        }
        progress?.Report(1m);
        (long lines, string firstName, string secondName, string eachMonth, string commonName) = state;
        return (lines, firstName, secondName, eachMonth, commonName);
    }

    public async Task<(long lines, string firstName, string secondName, string eachMonth, string commonName)> ReadAsync(IProgress<decimal>? progress = null, CancellationToken token = default)
    {
        //return await Task.Run(() => Read(progress), token);
        using StreamReader file = new(
            fileName,
            Encoding.ASCII,
            detectEncodingFromByteOrderMarks: false,
            new FileStreamOptions { Mode = Open, Access = FileAccess.Read, Share = FileShare.Read, Options = Asynchronous | SequentialScan });
        var (idx, process) = (0, Enumerable.Repeat(Task.CompletedTask, ThreadCnt).ToArray());
        State state = new();
        progress?.Report(0m);
        (var pos, int cnt) = (0m, ReportCnt);
        while (await file.ReadLineAsync(token) is { } line)
        {
            process[idx++] = state.ProcessLineAsync(line, token);
            if (idx == process.Length)
            {
                await Task.WhenAll(process);
                idx = 0;
            }
            pos += line.Length + 1;
            if (--cnt >= 0) continue;
            progress?.Report(pos / _size);
            cnt = ReportCnt;
        }
        progress?.Report(1m);
        (long lines, string firstName, string secondName, string eachMonth, string commonName) = state;
        return (lines, firstName, secondName, eachMonth, commonName);
    } 
}