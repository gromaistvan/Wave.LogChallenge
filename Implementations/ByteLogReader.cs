using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Wave.LogChallenge.Implementations;

internal sealed class ByteLogReader(string fileName, int bufferSize = 1_000_000): IAsyncLogReader
{
    private const byte LineEnd = (byte)'\n', Separator = (byte)'|', Zero = (byte)'0';

    private readonly long _size = new FileInfo(fileName).Length;

    private async IAsyncEnumerable<ReadOnlyMemory<byte>> ReadLineAsync([EnumeratorCancellation] CancellationToken token = default)
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
        int start = 0, maxCount, count;
        do
        {
            maxCount = bufferSize - start;
            count = await stream.ReadAsync(buffer.AsMemory(start, maxCount), token);
            var lastPos = 0;
            for (var i = 0; i < count; i += 1)
            {
                int pos = start + i;
                if (buffer[pos] != LineEnd) continue;
                yield return buffer.AsMemory(lastPos, pos - lastPos);
                lastPos = pos + 1;
            }
            Array.Copy(buffer, lastPos, buffer, 0, start + count - lastPos);
            start = start + count - lastPos;
        }
        while (count == maxCount);
        if (start > 0) yield return buffer.AsMemory(0, start);
    }

    public async Task<(long lines, string firstName, string secondName, string eachMonth, string commonName)> ReadAsync(IProgress<decimal> progress, CancellationToken token = default)
    {
        var monthsList = new long[12];
        var namesDict = new Dictionary<string, long>();
        var maxNameOccurrence = 1L;
        string commonName = "", firstName = "", secondName = "";
        var lines = 0L;
        var state = 0m;
        progress.Report(state / _size);
        await foreach (ReadOnlyMemory<byte> line in ReadLineAsync(token))
        {
            lines++;
            var start = 0;
            FindPos(line.Span, ref start, 4);
            int idx = (line.Span[start + 4] - Zero) * 10 + (line.Span[start + 5] - Zero) - 1;
            monthsList[idx]++;
            FindPos(line.Span, ref start, 3);
            int from = start;
            FindPos(line.Span, ref start, 1);
            int to = start - 1;
            string name = Encoding.ASCII.GetString(line.Span[from..to]);
            switch (lines)
            {
                case 432L:   firstName  = name; break;
                case 43243L: secondName = name; break;
            }
            if (namesDict.TryGetValue(name, out long value))
            {
                namesDict[name] = ++value;
                if (value > maxNameOccurrence) 
                {
                    maxNameOccurrence = value;
                    commonName = name;
                }
            }
            else
            {
                namesDict.Add(name, 1L);
            }
            state += line.Length + 1;
            if (lines % 100_000L == 0L)
            {
                progress.Report(state / _size);
            }
        }
        progress.Report(state / _size);
        return (lines, firstName, secondName, string.Join(',', monthsList), commonName);

        static void FindPos(ReadOnlySpan<byte> text, ref int start, int count)
        {
            for (var i = 0; i < count; i += 1)
            {
                var found = false;
                for (int idx = start; idx < text.Length; idx += 1)
                {
                    if (text[idx] != Separator) continue;
                    found = true;
                    start = idx + 1;
                    break;
                }
                if (!found) return;
            }
        }
    }
}