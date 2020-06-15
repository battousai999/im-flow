using Fclp;
using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Linq;
using System.Collections.Generic;
using static Battousai.Utils.ConsoleUtils;
using System.Diagnostics;

namespace im_flow
{
    class Program
    {
        private static Regex entryHeaderRegex = new Regex(@"^\[(\d{4}-\d{2}-\d{2}\s\d{2}:\d{2}:\d{2}.\d{4})\s\|\s+(\w+)\]\s([\w\.\[\]`]+)\s-\s(.*)$");

        static void Main(string[] args)
        {
            RunLoggingExceptions(() =>
            {
                // Setup command-line parsing
                var parser = new FluentCommandLineParser<Args>();

                parser.Setup(x => x.Filename)
                    .As('i', "input-file")
                    .Required()
                    .UseForOrphanArguments();

                parser.Setup(x => x.DisableAutoExpandConsole)
                    .As('x', "no-auto-expand-console")
                    .SetDefault(false);

                parser.Setup(x => x.OutputFilename)
                    .As('o', "output-file");

                parser.Setup(x => x.OpenInEditor)
                    .As('e', "open-in-editor")
                    .SetDefault(false);

                parser.Setup(x => x.IgnoreErrors)
                    .As("ignore-errors")
                    .SetDefault(false);

                var results = parser.Parse(args);

                if (results.HasErrors)
                {
                    Log("Invalid command-line parameters.");
                    Log(@"Example usage: .\im-flow.exe -i c:\some-folder\interceptor.log");
                    return;
                }

                var parameters = parser.Object;
                var filename = parameters.Filename;
                var autoExpand = !parameters.DisableAutoExpandConsole;
                var outputFilename = parameters.OutputFilename;
                var openInEditor = parameters.OpenInEditor;
                var ignoreErrors = parameters.IgnoreErrors;

                var content = File.ReadAllLines(filename);

                // Parse lines in log file into Entry objects...
                var entries = content.Aggregate(
                    new { LineNumber = 1, Results = new List<Entry>() },
                    (acc, x) =>
                    {
                        var match = entryHeaderRegex.Match(x);

                        if (match.Success)
                        {
                            var logDate = DateTimeOffset.Parse(match.Groups[1].Value);
                            var logLevel = match.Groups[2].Value;
                            var @namespace = match.Groups[3].Value;
                            var message = match.Groups[4].Value;

                            acc.Results.Add(new Entry(acc.LineNumber, logDate, logLevel, @namespace, message));
                        }
                        else
                        {
                            var lastEntry = acc.Results.LastOrDefault();

                            if (lastEntry == null)
                                throw new InvalidOperationException("First line must contain a entry header.");

                            lastEntry.ExtraLines.Add(x);
                        }

                        return new { LineNumber = acc.LineNumber + 1, acc.Results };
                    },
                    acc => acc.Results);

                // This will be used later (at some point) to provide payload details for messages
                AssociatePayloads(entries);

                if (openInEditor && String.IsNullOrWhiteSpace(outputFilename))
                {
                    outputFilename = $"{Path.GetTempFileName()}.txt";
                }

                var isOutputToConsole = String.IsNullOrWhiteSpace(outputFilename);
                var _writer = (isOutputToConsole ? null : new StreamWriter(outputFilename, false));

                try
                {
                    Action<string> write = text =>
                    {
                        if (_writer == null)
                            Console.Write(text);
                        else
                            _writer.Write(text);
                    };

                    Action<string> writeLine = text =>
                    {
                        if (_writer == null)
                            Console.WriteLine(text);
                        else
                            _writer.WriteLine(text);
                    };

                    Func<ConsoleColor, bool, Action<string>> buildColoredWriter = (color, withNewline) =>
                    {
                        Action<string> consoleWriter;
                        Action<string> streamWriter;

                        if (withNewline)
                        {
                            consoleWriter = Console.WriteLine;
                            streamWriter = text => _writer.WriteLine(text);
                        }
                        else
                        {
                            consoleWriter = Console.Write;
                            streamWriter = text => _writer.Write(text);
                        }

                        return text =>
                        {
                            if (_writer == null)
                            {
                                var saveColor = Console.ForegroundColor;
                                Console.ForegroundColor = color;
                                consoleWriter(text);
                                Console.ForegroundColor = saveColor;
                            }
                            else
                                streamWriter(text);
                        };
                    };

                    Action<string> writeError = buildColoredWriter(ConsoleColor.Red, true);
                    Action<string> writeWarning = buildColoredWriter(ConsoleColor.Yellow, true);
                    Action<string> writeSpecialInfo = buildColoredWriter(ConsoleColor.Cyan, true);
                    Action<string> writeEmphasized = buildColoredWriter(ConsoleColor.Magenta, false);

                    // Output the message flow to the console...
                    Func<Entry, bool> isError = entry => !ignoreErrors && (entry.IsError || entry.IsWarning);
                    var messageFlow = entries.Where(x => x.IsMessage || isError(x) || x.IsSpecialInfo).ToList();

                    var genesysMessages = messageFlow.Where(x => x.IsReceivedFromGenesys || x.IsSentToGenesys).ToList();
                    var fubuMessages = messageFlow.Where(x => x.IsReceivedFromFubu || x.IsSentToFubu || x.IsSentToTimService).ToList();
                    var sscMessages = messageFlow.Where(x => x.IsReceivedFromSsc || x.IsSentToSsc).ToList();

                    var maxGenesysMessageNameLength = genesysMessages.Max(x => x.GetGenesysMessage()?.Length ?? 0);
                    var maxSscMessageNameLength = sscMessages.Max(x => x.GetSscMessage()?.Length ?? 0);
                    var maxFubuMessageNameLength = fubuMessages.Max(x => (x.GetFubuMessage() ?? (x.GetTimServiceCall() + 2))?.Length ?? 0);

                    var dateFormat = "HH:mm:ss.ffff zzz";
                    var genesysPadding = Math.Max(maxGenesysMessageNameLength, 10);
                    var sscPadding = Math.Max(maxSscMessageNameLength, 10);
                    var lineNumberPadding = Math.Max(messageFlow.Max(x => x.LineNumber.ToString().Length), 7);
                    var datePadding = DateTimeOffset.Now.ToString(dateFormat).Length;
                    var fubuPadding = Math.Max(maxFubuMessageNameLength, 13);
                    var interceptorPadding = 17;
                    var neededWidth = lineNumberPadding + 1 + datePadding + 3 + genesysPadding + interceptorPadding + sscPadding + 1 + fubuPadding + 1;

                    if (autoExpand && isOutputToConsole)
                        Console.WindowWidth = neededWidth + 1;

                    write("Line #".PadRight(lineNumberPadding + 1));
                    write("Date".PadRight(datePadding + 3));
                    write("Genesys".PadRight(genesysPadding));
                    write("  (Interceptor)  ");
                    write("SSC".PadRight(sscPadding));
                    writeLine(" CoreBus/TIM");
                    writeLine(new String('=', neededWidth));

                    var nonGenesysInitialSpacing = new String(' ', genesysPadding);
                    var fubuAfterSpacing = new String(' ', sscPadding + 1);

                    messageFlow.ForEach(message =>
                    {
                        write(message.LineNumber.ToString().PadRight(lineNumberPadding));
                        write(" ");
                        write(message.LogDate.ToString(dateFormat));
                        write("   ");

                        if (message.IsError)
                        {
                            writeError($"ERROR: {message.LogMessage}");
                        }
                        else if (message.IsWarning)
                        {
                            writeWarning($"WARN: {message.LogMessage}");
                        }
                        else if (message.IsSpecialInfo)
                        {
                            writeSpecialInfo($"INFO: {message.GetSpecialInfoText()}");
                        }
                        else if (message.IsGenesysMessage)
                        {
                            var genesysMessage = message.GetGenesysMessage().PadRight(genesysPadding);

                            if (message.IsEmphasizedGenesysMessage)
                                writeEmphasized(genesysMessage);
                            else
                                write(genesysMessage);

                            writeLine(message.IsReceivedMessage ? "  ==>  | |       " : " <==   | |       ");
                        }
                        else if (message.IsSscMessage)
                        {
                            write(nonGenesysInitialSpacing);
                            write(message.IsSentMessage ? "       | |   ==> " : "       | |  <==  ");
                            writeLine(message.GetSscMessage());
                        }
                        else if (message.IsFubuMessage || message.IsSentToTimService)
                        {
                            write(nonGenesysInitialSpacing);
                            write(message.IsSentMessage ? "       | |   ==> " : "       | |  <==  ");
                            write(fubuAfterSpacing);
                            writeLine(message.GetFubuMessage() ?? ($"<{message.GetTimServiceCall()}>"));
                        }
                    });
                }
                finally
                {
                    if (_writer != null)
                    {
                        _writer.Flush();
                        _writer.Close();
                    }
                }

                if (openInEditor)
                {
                    new Process
                    {
                        StartInfo = new ProcessStartInfo(outputFilename)
                        {
                            UseShellExecute = true
                        }
                    }.Start();
                }
            },
            false,
            false);
        }

        public static void AssociatePayloads(List<Entry> entries)
        {
            var messageItems = entries.Select((x, i) => new { Index = i, Entry = x }).ToList();
            var alreadyAssociatedEntries = new HashSet<Entry>();

            foreach (var messageItem in messageItems)
            {
                IEnumerable<Entry> GetSubsequentEntries(Entry entry)
                {
                    foreach (var item in entries.SkipWhile(x => x != entry).Skip(1))
                    {
                        if (!alreadyAssociatedEntries.Contains(item))
                            yield return item;
                    }
                }

                var payload = GetSubsequentEntries(messageItem.Entry).FirstOrDefault(x => Entry.HasPayloadFor(x, messageItem.Entry));

                if (payload != null)
                {
                    messageItem.Entry.PayloadEntry = payload;
                    alreadyAssociatedEntries.Add(payload);
                }
            }
        }
    }
}
