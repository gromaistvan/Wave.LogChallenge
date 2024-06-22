using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Wave.LogChallenge.Implementations;

internal class FastCharLogReader(string fileName): IAsyncLogReader
{
    private const char Separator = '|', Zero = '0';

    private readonly long _size = new FileInfo(fileName).Length;

    public async Task<(long lines, string firstName, string secondName, string eachMonth, string commonName)> ReadAsync(IProgress<decimal>? progress = null, CancellationToken token = default)
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
        progress?.Report(state / _size);
        while (await file.ReadLineAsync(token) is { } line)
        {
            lines++;
            state += line.Length + 1;
            if (lines % 100_000L == 0L)
            {
                progress?.Report(state / _size);
            }
        }
        progress?.Report(state / _size);
        return (lines, firstName, secondName, string.Join(',', monthsList), commonName);
    }
}