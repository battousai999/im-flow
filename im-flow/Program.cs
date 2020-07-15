using Fclp;
using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Linq;
using System.Collections.Generic;
using static Battousai.Utils.ConsoleUtils;
using System.Diagnostics;
using System.Globalization;

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

                parser.Setup(x => x.Filenames)
                    .As('i', "input-file")
                    .Required()
                    .UseForOrphanArguments()
                    .WithDescription("<filename(s)>  The names of the files to process (required)");

                parser.Setup(x => x.DisableAutoExpandConsole)
                    .As('x', "no-auto-expand-console")
                    .SetDefault(false)
                    .WithDescription("            Suppress expanding the width of the console to fit the content");

                parser.Setup(x => x.OutputFilename)
                    .As('o', "output-file")
                    .WithDescription("<filename>  Write output to a file with a given name");

                parser.Setup(x => x.OpenInEditor)
                    .As('e', "open-in-editor")
                    .SetDefault(false)
                    .WithDescription("            Open the output in an editor");

                parser.Setup(x => x.IgnoreErrors)
                    .As("ignore-errors")
                    .SetDefault(false)
                    .WithDescription("            Suppress display of errors in output");

                parser.Setup(x => x.SuppressAnnotations)
                    .As('a', "suppress-annotations")
                    .SetDefault(false)
                    .WithDescription("            Suppress display of annotations in output");

                parser.Setup(x => x.IncludeHeartbeat)
                    .As('h', "include-heartbeat")
                    .SetDefault(false)
                    .WithDescription("            Include Genesys heartbeat messages (EventAddressInfo) in output");

                parser.Setup(x => x.ParseLogDatesAsLocal)
                    .As('l', "local-dates")
                    .SetDefault(false)
                    .WithDescription("            Parse log dates as local instead of UTC");

                parser.Setup(x => x.MatchMessages)
                    .As('m', "match-messages")
                    .WithDescription("            Highlight messages that match given names");

                var results = parser.Parse(args);

                if (results.HasErrors)
                {
                    Log("Invalid command-line parameters.");
                    Log(@"Example usage: .\im-flow.exe -i c:\some-folder\interceptor.log");
                    Log();

                    var longNamePadding = parser.Options.Max(x => x.LongName.Length);

                    parser.Options
                        .ToList()
                        .ForEach(x =>
                        {
                            Log($"   {(x.HasShortName ? $"-{x.ShortName}, " : "    ")}--{x.LongName.PadRight(longNamePadding)} {x.Description}");
                        });

                    Log();
                    
                    return;
                }

                var parameters = parser.Object;
                var filenames = parameters.Filenames;
                var autoExpand = !parameters.DisableAutoExpandConsole;
                var outputFilename = parameters.OutputFilename;
                var openInEditor = parameters.OpenInEditor;
                var ignoreErrors = parameters.IgnoreErrors;
                var suppressAnnotations = parameters.SuppressAnnotations;
                var includeHeartbeat = parameters.IncludeHeartbeat;
                var parseDatesAsLocal = parameters.ParseLogDatesAsLocal;
                var areMultipleFiles = filenames.Count > 1;
                var matchMessages = parameters.MatchMessages;

                var content = filenames
                    .SelectMany(x => x.ContainsWildcards() ? EnumerateFiles(x) : x.ToSingleton())
                    .SelectMany(x =>
                    {
                        var values = File.ReadAllLines(x).Select((y, i) => new { LineNumber = i + 1, LineText = y });

                        return values.Select(value => new { Filename = x, value.LineNumber, value.LineText });
                    });

                DateTimeStyles dateTimeStyles = (parseDatesAsLocal ? DateTimeStyles.AssumeLocal : DateTimeStyles.AssumeUniversal);

                // Parse lines in log file into Entry objects...
                var entries = content.Aggregate(
                    new List<Entry>(),
                    (acc, x) =>
                    {
                        var lineNumber = x.LineNumber;
                        var text = x.LineText;
                        var filename = x.Filename;
                        var match = entryHeaderRegex.Match(text);

                        if (match.Success)
                        {
                            var logDate = DateTimeOffset.Parse(match.Groups[1].Value, null, dateTimeStyles);
                            var logLevel = match.Groups[2].Value;
                            var @namespace = match.Groups[3].Value;
                            var message = match.Groups[4].Value;

                            acc.Add(new Entry(filename, lineNumber, logDate, logLevel, @namespace, message));
                        }
                        else
                        {
                            var lastEntry = acc.LastOrDefault();

                            if (lastEntry == null)
                                throw new InvalidOperationException("First line must contain a entry header.");

                            lastEntry.ExtraLines.Add(text);
                        }

                        return acc;
                    });

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
                    Action<string> writeMessageHighlight = buildColoredWriter(ConsoleColor.Green, false);

                    Action<int> writeSpaces = num => write(new String(' ', num));

                    Func<string, bool> isMessageHighlightMatch = message =>
                    {
                        if ((matchMessages?.Count ?? 0) == 0)
                            return false;

                        return matchMessages.Contains(message, StringComparer.OrdinalIgnoreCase);
                    };

                    // Output the message flow to the console...
                    Func<Entry, bool> isError = entry => !ignoreErrors && (entry.IsError || entry.IsFatal || entry.IsWarning);

                    var messageFlow = entries
                        .Where(x => x.IsMessage || isError(x) || x.IsSpecialInfo)
                        .OrderBy(x => x.LogDate)
                        .ThenBy(x => x, LogEntryComparer.Default)
                        .ToList();

                    if (!includeHeartbeat)
                    {
                        messageFlow = messageFlow
                        .Where(x => !StringComparer.OrdinalIgnoreCase.Equals(x.GetGenesysMessage(), "EventAddressInfo"))
                        .ToList();
                    }

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
                    var annotationBarPadding = lineNumberPadding + 1 + datePadding + 3 + genesysPadding + ((interceptorPadding - 3) / 2);

                    // Expand console width, if applicable
                    if (autoExpand && isOutputToConsole)
                        Console.WindowWidth = Math.Max(neededWidth + 1, Console.WindowWidth);

                    // Write header
                    write("Line #".PadRight(lineNumberPadding + 1));
                    write("Date".PadRight(datePadding + 3));
                    write("Genesys".PadRight(genesysPadding));
                    write("  (Interceptor)  ");
                    write("SSC".PadRight(sscPadding));
                    writeLine(" CoreBus/TIM");
                    writeLine(new String('=', neededWidth));

                    var nonGenesysInitialSpacing = new String(' ', genesysPadding);
                    var fubuAfterSpacing = new String(' ', sscPadding + 1);

                    string currentFilename = null;

                    // Write lines with message information
                    messageFlow.ForEach(message =>
                    {
                        if (areMultipleFiles && !StringComparer.OrdinalIgnoreCase.Equals(currentFilename, message.Filename))
                        {
                            currentFilename = message.Filename;
                            var bar = new String('-', currentFilename.Length);
                            writeLine($"\n{bar}\n{Path.GetFileName(message.Filename)}\n{bar}");
                        }

                        write(message.LineNumber.ToString().PadRight(lineNumberPadding));
                        write(" ");
                        write(message.LogDate.ToLocalTime().ToString(dateFormat));
                        write("   ");

                        if (message.IsError)
                        {
                            writeError($"ERROR: {message.LogMessage}");
                        }
                        else if (message.IsFatal)
                        {
                            writeError($"FATAL: {message.LogMessage}");
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
                            var genesysMessage = message.GetGenesysMessage();

                            if (isMessageHighlightMatch(genesysMessage))
                                writeMessageHighlight(genesysMessage.PadRight(genesysPadding));
                            else if (message.IsEmphasizedGenesysMessage)
                                writeEmphasized(genesysMessage.PadRight(genesysPadding));
                            else
                                write(genesysMessage.PadRight(genesysPadding));

                            writeLine(message.IsReceivedMessage ? "  ==>  | |       " : " <==   | |       ");

                            if (message.HasAnnotation && !suppressAnnotations)
                            {
                                writeSpaces(annotationBarPadding);
                                write("| |");
                                Console.SetCursorPosition(0, Console.CursorTop);
                                writeSpaces(lineNumberPadding + 1 + datePadding + 3);

                                if (message.IsEmphasizedGenesysMessage)
                                    writeEmphasized(message.Annotation);
                                else
                                    write(message.Annotation);

                                writeLine("");
                            }
                        }
                        else if (message.IsSscMessage)
                        {
                            write(nonGenesysInitialSpacing);
                            write(message.IsSentMessage ? "       | |   ==> " : "       | |  <==  ");

                            var sscMessage = message.GetSscMessage();

                            if (isMessageHighlightMatch(sscMessage))
                            {
                                writeMessageHighlight(sscMessage);
                                writeLine("");
                            }
                            else
                                writeLine(sscMessage);

                            if (message.HasAnnotation && !suppressAnnotations)
                            {
                                writeSpaces(lineNumberPadding + 1 + datePadding + 3);
                                write(nonGenesysInitialSpacing);
                                write(message.IsSentMessage ? "       | |       " : "       | |       ");
                                writeLine(message.Annotation);
                            }
                        }
                        else if (message.IsFubuMessage || message.IsSentToTimService)
                        {
                            write(nonGenesysInitialSpacing);
                            write(message.IsSentMessage ? "       | |   ==> " : "       | |  <==  ");
                            write(fubuAfterSpacing);

                            var fubuMessage = message.GetFubuMessage() ?? $"<{message.GetTimServiceCall()}>";

                            if (isMessageHighlightMatch(fubuMessage))
                            {
                                writeMessageHighlight(fubuMessage);
                                writeLine("");
                            }
                            else
                                writeLine(fubuMessage);

                            if (message.HasAnnotation && !suppressAnnotations)
                            {
                                writeSpaces(lineNumberPadding + 1 + datePadding + 3);
                                write(nonGenesysInitialSpacing);
                                write(message.IsSentMessage ? "       | |       " : "       | |       ");
                                write(fubuAfterSpacing);
                                writeLine(message.Annotation);
                            }
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

        private static IEnumerable<string> EnumerateFiles(string wildcardFilename)
        {
            var path = Path.GetDirectoryName(wildcardFilename);
            var searchPattern = Path.GetFileName(wildcardFilename);

            if (String.IsNullOrWhiteSpace(path))
                path = @".\";

            return Directory.GetFiles(path, searchPattern);
        }

        public static void AssociatePayloads(List<Entry> entries)
        {
            var messageItems = entries.Select((x, i) => new { Index = i, Entry = x }).ToList();
            var alreadyAssociatedEntries = new HashSet<Entry>();

            foreach (var messageItem in messageItems)
            {
                IEnumerable<Entry> GetSubsequentEntries(Entry entry)
                {
                    foreach (var item in entries.SkipWhile(x => x != entry).Skip(1).Take(2000))
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
