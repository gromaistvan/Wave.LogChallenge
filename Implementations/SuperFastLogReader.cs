﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace Wave.LogChallenge.Implementations;

internal sealed class SuperFastLogReader(string fileName, int bufferSize = 1_000_000): IAsyncLogReader
{
    private const byte LineEnd = (byte)'\n', Separator = (byte)'|', Zero = (byte)'0';

    private readonly long _size = new FileInfo(fileName).Length;

    public async Task<(long lines, string firstName, string secondName, string eachMonth, string commonName)> ReadAsync(IProgress<decimal> progress, CancellationToken token = default)
    {
        var buffer = new byte[bufferSize];
        var monthsList = new long[12];
        var names = Channel.CreateUnbounded<string>();
        ChannelWriter<string> nameWriter = names.Writer;
        Task<string> namesWorker = Task.Run(async delegate
        {
            var namesDict = new Dictionary<string, long>(10_000);
            var maxNameOccurrence = 1L;
            var commonName = "";
            await foreach (string name in names.Reader.ReadAllAsync(token))
            {
                if (namesDict.TryGetValue(name, out long value))
                {
                    namesDict[name] = ++value;
                    if (value <= maxNameOccurrence) continue;
                    maxNameOccurrence = value;
                    commonName = name;
                }
                else
                {
                    namesDict.Add(name, 1L);
                }
            }
            return commonName;
        }, token);
        string firstName = "", secondName = "";
        await using FileStream stream = File.Open(
            fileName,
            new FileStreamOptions
            {
                Mode = FileMode.Open,
                Access = FileAccess.Read,
                Share = FileShare.Read,
                Options = FileOptions.SequentialScan | FileOptions.Asynchronous
            });
        var lines = 1L;
        int pos = 0, column = 0, nameStart = 0;
        var state = 0m;
        progress.Report(state / _size);
        for (int start = 0, count = await stream.ReadAsync(buffer.AsMemory(0, bufferSize), token);
             count > 0 && ! token.IsCancellationRequested;
             count = await stream.ReadAsync(buffer.AsMemory(start, bufferSize - start), token))
        {
            int end = start + count;
            bool last = end < bufferSize;
            while (buffer[--end] != LineEnd) { }
            for (var i = 0; i < end; ++i, ++pos)
            {
                byte current = buffer[i];
                if (column < 8)
                {
                    if (current != Separator) continue;
                    switch (++column)
                    {
                        case 4:
                            int idx = (buffer[i + 5] - Zero) * 10 + (buffer[i + 6] - Zero) - 1;
                            monthsList[idx]++;
                            Jump(19, 1);
                            break;
                        case 7:
                            nameStart = i + 1;
                            break;
                        case 8:
                            string name = Encoding.ASCII.GetString(buffer[nameStart..i]);
                            switch (lines)
                            {
                                case 432L:   firstName  = name; break;
                                case 43243L: secondName = name; break;
                            }
                            await nameWriter.WriteAsync(name, token);
                            Jump(100 - pos);
                            break;
                    }
                    continue;
                }
                if (current != LineEnd) continue;
                lines++;
                Set(13, 2);
                continue;

                void Jump(int cnt, int cls = 0)
                {
                    i += cnt;
                    pos += cnt;
                    if (cls == 0) return;
                    column += cls;
                }

                void Set(int cnt, int cls)
                {
                    i += cnt;
                    pos = cnt;
                    column = cls;
                }
            }
            state += end;
            progress.Report(state / _size);
            if (last) break;
            Array.Copy(buffer, end, buffer, 0, start = start + count - end);
        }
        nameWriter.Complete();
        return (lines, firstName, secondName, string.Join(',', monthsList), await namesWorker);
    }

    public (long lines, string firstName, string secondName, string eachMonth, string commonName) Read(IProgress<decimal> progress)
    {
        var buffer = new byte[bufferSize];
        var monthsList = new long[12];
        var namesDict = new Dictionary<string, long>(10_000);
        var maxNameOccurrence = 1L;
        string commonName = "", firstName = "", secondName = "";
        using FileStream stream = File.Open(
            fileName,
            new FileStreamOptions
            {
                Mode = FileMode.Open,
                Access = FileAccess.Read,
                Share = FileShare.Read,
                Options = FileOptions.SequentialScan
            });
        var lines = 1L;
        int pos = 0, column = 0, nameStart = 0;
        var state = 0m;
        progress.Report(state / _size);
        for (int start = 0, count = stream.Read(buffer, 0, bufferSize);
             count > 0;
             count = stream.Read(buffer, start, bufferSize - start))
        {
            int end = start + count;
            bool last = end < bufferSize;
            while (buffer[--end] != LineEnd) { }
            for (var i = 0; i < end; ++i, ++pos)
            {
                byte current = buffer[i];
                if (column < 8)
                {
                    if (current != Separator) continue;
                    switch (++column)
                    {
                        case 4:
                            int idx = (buffer[i + 5] - Zero) * 10 + (buffer[i + 6] - Zero) - 1;
                            monthsList[idx]++;
                            Jump(19, 1);
                            break;
                        case 7:
                            nameStart = i + 1;
                            break;
                        case 8:
                            string name = Encoding.ASCII.GetString(buffer[nameStart..i]);
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
                            Jump(100 - pos);
                            break;
                    }
                    continue;
                }
                if (current != LineEnd) continue;
                lines++;
                Set(13, 2);
                continue;

                void Jump(int cnt, int cls = 0)
                {
                    i += cnt;
                    pos += cnt;
                    if (cls == 0) return;
                    column += cls;
                }

                void Set(int cnt, int cls)
                {
                    i += cnt;
                    pos = cnt;
                    column = cls;
                }
            }
            state += end;
            progress.Report(state / _size);
            if (last) break;
            Array.Copy(buffer, end, buffer, 0, start = start + count - end);
        }
        return (lines, firstName, secondName, string.Join(',', monthsList), commonName);
    }
}
