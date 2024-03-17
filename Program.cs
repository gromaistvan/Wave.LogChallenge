/*
c1 – Write a program that will print out the total number of lines in the file.
c2 – Notice that the 8th column contains a person’s name. Write a program that loads in this data and creates an array with all name strings. Print out the 432nd and 43243rd names.
c3 – Notice that the 5th column contains a form of date. Count how many donations occurred in each month and print out the results.
c4 – Notice that the 8th column contains a person’s name. Create an array with each first name. Identify the most common first name in the data and how many times it occurs.
*/

using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;

var watch = Stopwatch.StartNew();
await foreach (Memory<byte> line in ReadLineAsync("test.txt", 10))
{
    Console.WriteLine(Encoding.ASCII.GetString(line.Span));
}
/*
var (n, first, secound, eachMonth, maxName) = await ReadFileAsync("itcont.txt");
watch.Stop();
Console.WriteLine(n);
Console.WriteLine(first);
Console.WriteLine(secound);
Console.WriteLine(eachMonth);
Console.WriteLine(maxName);
*/
Console.WriteLine(watch.Elapsed);
return;

#pragma warning disable CS8321 // Local function is declared but never used
static async Task<(long count, string first, string secound, string eachMonth, string maxName)> ReadFileAsync(string fileName)
{
    using StreamReader file = new (
        fileName,
        Encoding.ASCII,
        detectEncodingFromByteOrderMarks: false,
        new FileStreamOptions
        {
            Access = FileAccess.Read,
            Mode = FileMode.Open,
            Options = FileOptions.SequentialScan | FileOptions.Asynchronous,
            Share = FileShare.None,
            BufferSize = 10 * 1024 * 1024
        });
    long n = 0, max = 1;
    string? first = null, second = null, maxName = null;
    var months = new int[12];
    Dictionary<string, int> names = [];
    while (await file.ReadLineAsync() is { } line)
    {
        switch (++n)
        {
            case 432:
                first = line;
                break;
            case 43243:
                second = line;
                break;
        }
        var start = 0;
        FindPos(line, '|', 4, ref start);
        SetMonth(ref months, line.AsSpan((start+4)..(start+6)));
        FindPos(line, '|', 3, ref start);
        int from = start + 1;
        FindPos(line, ',', 1, ref start);
        int to = start - 1;
        if (to <= from) continue;
        string name = line[from..to];
        if (names.TryGetValue(name, out int count))
        {
            names[name] = ++count;
            if (count <= max) continue;
            max = count;
            maxName = name;
        }
        else
        {
            names.Add(name, 1);
        }
    }
    return (n, first ?? "", second ?? "", string.Join(',', months), maxName ?? "");
}
#pragma warning restore CS8321 // Local function is declared but never used

static void SetMonth(ref int[] months, ReadOnlySpan<char> month) =>
    months[(month[0] - '0') * 10 + (month[1] - '0') - 1] += 1;

static void FindPos(ReadOnlySpan<char> text, char chr, int count, ref int start)
{
    for (var i = 0; i < count; i += 1)
    {
        var found = false;
        for (int idx = start; idx < text.Length; idx += 1)
        {
            if (text[idx] != chr) continue;
            found = true;
            start = idx + 1;
            break;
        }
        if (! found) return;
    }
}

static async IAsyncEnumerable<Memory<byte>> ReadLineAsync(string fileName, int bufferSize = 10 * 1024 * 1024, [EnumeratorCancellation] CancellationToken token = default)
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
            Options = FileOptions.SequentialScan | FileOptions.Asynchronous,
            BufferSize = buffer.Length
        });
    var start = 0;
    while (true)
    {
        int count = await stream.ReadAsync(buffer.AsMemory(start, buffer.Length - start), token);
        var pos = 0;
        for (int i = start; i < count; i += 1)
        {
            if (buffer[i] != '\n') continue;
            yield return buffer.AsMemory(pos, i - pos);
            pos = i + 1;
        }
        Array.Copy(buffer, pos, buffer, 0, count - pos);
        start = count - pos;
        if (count == buffer.Length) continue;
        yield return buffer.AsMemory(0, start);
        yield break;
    }
}