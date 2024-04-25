using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Wave.LogChallenge;

internal class LogReaderAlt(string fileName, int bufferSize = 64 * 1024 * 1024): ILogReader
{
    private const char Separator = '|';

    public async Task<(long lines, string firstName, string secoundName, string eachMonth, string commonName)> ReadAsync(CancellationToken token = default)
    {
        var months = new long[12];
        var lines = 0L;
        var max = 1L;
        var names = new Dictionary<string, long>();
        var firstName = "";
        var secondName = "";
        var commonName = "";
        using StreamReader file = new (
            fileName,
            Encoding.ASCII,
            detectEncodingFromByteOrderMarks: false,
            new FileStreamOptions
            {
                Mode = FileMode.Open,
                Access = FileAccess.Read,
                Share = FileShare.None,
                Options = FileOptions.Asynchronous,
                BufferSize = bufferSize
            });
        while (await file.ReadLineAsync(token) is { } line)
        {
            lines++;
            var start = 0;
            FindPos(line, 4, ref start);
            months[(line[start + 4] - '0') * 10 + (line[start + 5] - '0') - 1]++;
            FindPos(line, 3, ref start);
            int from = start + 1;
            FindPos(line, 1, ref start);
            int to = start - 1;
            if (to <= from) continue;
            string name = line[from..to];
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