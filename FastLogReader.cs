using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Wave.LogChallenge;

internal sealed class FastLogReader(string fileName, int bufferSize = 64 * 1024 * 1024): ILogReader
{
    private const byte LineEnd = (byte)'\n';

    private const byte Separator = (byte)'|';

    private const byte Zero = (byte)'0';

    private async IAsyncEnumerable<ReadOnlyMemory<byte>> ReadChunkAsync([EnumeratorCancellation] CancellationToken token = default)
    {
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
        for (int start = 0, count = await stream.ReadAsync(buffer.AsMemory(start, bufferSize), token);
             count > 0;
             count = await stream.ReadAsync(buffer.AsMemory(start, bufferSize - start), token))
        {
            int end = start + count;
            while (buffer[--end] != LineEnd) { }
            yield return buffer.AsMemory(0, end);
            Array.Copy(buffer, end, buffer, 0, start + count - end);
            start = start + count - end;
        }
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
        var column = 0;
        var nameStart = 0;
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
                                months[(chunk.Span[i + 4] - Zero) * 10 + (chunk.Span[i + 5] - Zero) - 1]++;
                                break;
                            case 7:
                                nameStart = i + 1;
                                break;
                            case 8:
                                string name = Encoding.ASCII.GetString(chunk.Span[nameStart..i]);
                                switch (lines)
                                {
                                    case 432L:   firstName = name;  break;
                                    case 43243L: secondName = name; break;
                                }
                                if (names.TryGetValue(name, out long value))
                                {
                                    names[name] = ++value;
                                }
                                else
                                {
                                    names.Add(name, value = 1L);
                                }
                                if (value > max)
                                {
                                    max = value;
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
        }
        return (lines, firstName, secondName, string.Join(',', months), commonName);
    }
}
