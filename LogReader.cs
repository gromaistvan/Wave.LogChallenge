using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Wave.LogChallenge;

internal sealed class LogReader(string fileName, int bufferSize = 64 * 1024 * 1024): ILogReader
{
    private const byte LineEnd = (byte)'\n';

    private const byte Separator = (byte)'|';

    private async IAsyncEnumerable<ReadOnlyMemory<byte>> ReadLineAsync([EnumeratorCancellation] CancellationToken token = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(fileName);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(bufferSize, 0);
        var buffer = new byte[bufferSize];
        await using FileStream stream = File.Open(
            fileName,
            new FileStreamOptions
            {
                Mode = FileMode.Open,
                Access = FileAccess.Read,
                Share = FileShare.None,
                Options = FileOptions.Asynchronous,
                BufferSize = bufferSize
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

    public async Task<(long lines, string firstName, string secoundName, string eachMonth, string commonName)> ReadAsync(CancellationToken token = default)
    {
        var months = new long[12];
        var lines = 0L;
        var max = 1L;
        var names = new Dictionary<string, long>();
        var firstName = "";
        var secondName = "";
        var commonName = "";
        await foreach (ReadOnlyMemory<byte> line in ReadLineAsync(token))
        {
            lines++;
            var start = 0;
            FindPos(line.Span, ref start, 4);
            months[(line.Span[start + 4] - (byte)'0') * 10 + (line.Span[start + 5] - (byte)'0') - 1]++;
            FindPos(line.Span, ref start, 3);
            int from = start + 1;
            FindPos(line.Span, ref start, 1);
            int to = start - 1;
            if (to <= from) continue;
            string name = Encoding.ASCII.GetString(line.Span[from..to]);
            switch (lines)
            {
                case 432L: firstName = name; break;
                case 43243L: secondName = name; break;
            }
            if (names.TryGetValue(name, out long value))
            {
                names[name] = ++value;
                if (value <= max) continue;
                max = value;
                commonName = name;
            }
            else
            {
                names.Add(name, 1L);
            }
        }
        return (lines, firstName, secondName, string.Join(',', months), commonName);

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
                if (! found) return;
            }
        }
    }
}