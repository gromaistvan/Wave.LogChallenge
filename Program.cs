/*
– Write a program that will print out the total number of lines in the file.
– Notice that the 8th column contains a person’s name. Write a program that loads in this data and creates an array with all name strings.
  Print out the 432nd and 43243rd names.
– Notice that the 5th column contains a form of date. Count how many donations occurred in each month and print out the results.
– Notice that the 8th column contains a person’s name. Create an array with each first name. Identify the most common first name in
  the data and how many times it occurs.
*/

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

{
    var watch = Stopwatch.StartNew();
    var (count, firstName, secoundName, eachMonth, commonName) = await ReadFile1Async("itcont.txt");
    watch.Stop();
    Console.WriteLine(count);
    Console.WriteLine(firstName);
    Console.WriteLine(secoundName);
    Console.WriteLine(eachMonth);
    Console.WriteLine(commonName);
    Console.WriteLine(watch.Elapsed);
}

{
    var watch = Stopwatch.StartNew();
    var (count, firstName, secoundName, eachMonth, commonName) = await ReadFile2Async("itcont.txt");
    watch.Stop();
    Console.WriteLine(count);
    Console.WriteLine(firstName);
    Console.WriteLine(secoundName);
    Console.WriteLine(eachMonth);
    Console.WriteLine(commonName);
    Console.WriteLine(watch.Elapsed);
}

return;

static async Task<(long count, string firstName, string secoundName, string eachMonth, string commonName)> ReadFile1Async(string fileName, int bufferSize = 64 * 1024 * 1024, CancellationToken token = default)
{
    const char separator = '|';
    var months = new long[12];
    var count = 0L;
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
        count++;
        int start = 0;
        FindPos(line, 4, ref start);
        months[(line[start + 4] - '0') * 10 + (line[start + 5] - '0') - 1]++;
        FindPos(line, 3, ref start);
        int from = start + 1;
        FindPos(line, 1, ref start);
        int to = start - 1;
        if (to <= from) continue;
        string name = line[from..to];
        switch (count)
        {
            case 432:   firstName = name;  break;
            case 43243: secondName = name; break;
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
    return (count, firstName, secondName, string.Join(',', months), commonName);

    void FindPos(ReadOnlySpan<char> text, int count, ref int start)
    {
        for (var i = 0; i < count; i += 1)
        {
            var found = false;
            for (int idx = start; idx < text.Length; idx += 1)
            {
                if (text[idx] != separator) continue;
                found = true;
                start = idx + 1;
                break;
            }
            if (! found) return;
        }
    }
}

static async IAsyncEnumerable<ReadOnlyMemory<byte>> ReadLineAsync(string fileName, int bufferSize = 64 * 1024 * 1024, [EnumeratorCancellation] CancellationToken token = default)
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
        int lastPos = 0;
        for (int i = 0; i < count; i += 1)
        {
            int pos = start + i;
            if (buffer[pos] != '\n') continue;
            yield return buffer.AsMemory(lastPos, pos - lastPos);
            lastPos = pos + 1;
        }
        Array.Copy(buffer, lastPos, buffer, 0, start + count - lastPos);
        start = start + count - lastPos;
    }
    while (count == maxCount);
    if (start > 0) yield return buffer.AsMemory(0, start);
}

static async Task<(long count, string firstName, string secoundName, string eachMonth, string commonName)> ReadFile2Async(string fileName, CancellationToken token = default)
{
    const byte separator = (byte)'|';
    var months = new long[12];
    var count = 0L;
    var max = 1L;
    var names = new Dictionary<string, long>();
    var firstName = "";
    var secondName = "";
    var commonName = "";

    await foreach (ReadOnlyMemory<byte> line in ReadLineAsync(fileName).WithCancellation(token))
    {
        count++;
        int start = 0;
        FindPos(line.Span, ref start, 4);
        months[(line.Span[start + 4] - (byte)'0') * 10 + (line.Span[start + 5] - (byte)'0') - 1]++;
        FindPos(line.Span, ref start, 3);
        int from = start + 1;
        FindPos(line.Span, ref start, 1);
        int to = start - 1;
        if (to <= from) continue;
        string name = Encoding.ASCII.GetString(line.Span[from..to]);
        switch (count)
        {
            case 432:   firstName = name;  break;
            case 43243: secondName = name; break;
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
    return (count, firstName, secondName, string.Join(',', months), commonName);

    void FindPos(ReadOnlySpan<byte> text, ref int start, int count)
    {
        for (var i = 0; i < count; i += 1)
        {
            var found = false;
            for (int idx = start; idx < text.Length; idx += 1)
            {
                if (text[idx] != separator) continue;
                found = true;
                start = idx + 1;
                break;
            }
            if (! found) return;
        }
    }
}