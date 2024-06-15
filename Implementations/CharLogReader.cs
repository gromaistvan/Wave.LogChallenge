using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Wave.LogChallenge.Implementations;

internal class CharLogReader(string fileName): IAsyncLogReader
{
    private const char Separator = '|', Zero = '0';

    private readonly long _size = new FileInfo(fileName).Length;

    public async Task<(long lines, string firstName, string secondName, string eachMonth, string commonName)> ReadAsync(IProgress<decimal> progress, CancellationToken token = default)
    {
        var monthsList = new long[12];
        var namesDict = new Dictionary<string, long>();
        var maxNameOccurrence = 1L;
        string commonName = "", firstName = "", secondName = "";
        using StreamReader file = new(
            fileName,
            Encoding.ASCII,
            detectEncodingFromByteOrderMarks: false,
            new FileStreamOptions
            {
                Mode = FileMode.Open,
                Access = FileAccess.Read,
                Share = FileShare.Read,
                Options = FileOptions.SequentialScan | FileOptions.Asynchronous
            });
        var lines = 0L;
        var state = 0m;
        progress.Report(state / _size);
        while (await file.ReadLineAsync(token) is { } line)
        {
            lines++;
            var start = 0;
            FindPos(line, 4, ref start);
            int idx = (line[start + 4] - Zero) * 10 + (line[start + 5] - Zero) - 1;
            monthsList[idx]++;
            FindPos(line, 3, ref start);
            int from = start;
            FindPos(line, 1, ref start);
            int to = start - 1;
            string name = line[from..to];
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

        static void FindPos(ReadOnlySpan<char> text, int count, ref int start)
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