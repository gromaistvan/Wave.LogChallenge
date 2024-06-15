using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Wave.LogChallenge.Implementations;

internal sealed class FastLogReader(string fileName, int bufferSize = 1_000_000): IAsyncLogReader
{
    private const byte LineEnd = (byte)'\n', Separator = (byte)'|', Zero = (byte)'0';

    private readonly long _size = new FileInfo(fileName).Length;

    private async IAsyncEnumerable<ReadOnlyMemory<byte>> ReadChunkAsync([EnumeratorCancellation] CancellationToken token = default)
    {
        var buffer = new byte[bufferSize];
        await using FileStream stream = File.Open(
            fileName,
            new FileStreamOptions
            {
                Mode = FileMode.Open,
                Access = FileAccess.Read,
                Share = FileShare.Read,
                Options = FileOptions.SequentialScan | FileOptions.Asynchronous
            });
        for (int start = 0, count = await stream.ReadAsync(buffer.AsMemory(0, bufferSize), token);
             count > 0 && ! token.IsCancellationRequested;
             count = await stream.ReadAsync(buffer.AsMemory(start, bufferSize - start), token))
        {
            int end = start + count;
            bool last = end < bufferSize;
            while (! last && buffer[--end] != LineEnd) { }
            yield return buffer.AsMemory(0, end);
            if (last) break;
            Array.Copy(buffer, end, buffer, 0, start + count - end);
            start = start + count - end;
        }
    }

    public async Task<(long lines, string firstName, string secondName, string eachMonth, string commonName)> ReadAsync(IProgress<decimal> progress, CancellationToken token = default)
    {
        var monthsList = new long[12];
        var namesDict = new Dictionary<string, long>();
        var maxNameOccurrence = 1L;
        string commonName = "", firstName = "", secondName = "";
        var lines = 0L;
        int column = 0, nameStart = 0;
        var state = 0m;
        progress.Report(state / _size);
        await foreach (ReadOnlyMemory<byte> chunk in ReadChunkAsync(token))
        {
            for (var i = 0; i < chunk.Length; ++i)
            {
                byte current = chunk.Span[i];
                switch (current)
                {
                    case Separator:
                        switch (++column)
                        {
                            case 4:
                                int monthIndex = (chunk.Span[i + 5] - Zero) * 10 + (chunk.Span[i + 6] - Zero) - 1;
                                monthsList[monthIndex]++;
                                break;
                            case 7:
                                nameStart = i + 1;
                                break;
                            case 8:
                                string name = Encoding.ASCII.GetString(chunk.Span[nameStart..i]);
                                switch (lines)
                                {
                                    case 432L - 1:   firstName  = name; break;
                                    case 43243L - 1: secondName = name; break;
                                }
                                if (namesDict.TryGetValue(name, out long value))
                                {
                                    namesDict[name] = ++value;
                                }
                                else
                                {
                                    namesDict.Add(name, value = 1L);
                                }
                                if (value > maxNameOccurrence)
                                {
                                    maxNameOccurrence = value;
                                    commonName = name;
                                }
                                break;
                        }
                        break;
                    case LineEnd:
                        lines++;
                        column = 0;
                        break;
                }
            }
            state += chunk.Length;
            progress.Report(state / _size);
        }
        return (lines, firstName, secondName, string.Join(',', monthsList), commonName);
    }
}
