using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace im_flow
{
    public class Entry
    {
        public static readonly Regex sentToSscRegex = new Regex(@"Sending\s<([\w\d.]+)>\smessage\sto\s<SSC>", RegexOptions.IgnoreCase);
        public static readonly Regex sentToFubuRegex = new Regex(@"Sending\s<([\w\d.]+)>\smessage\sto\s<CoreBus>", RegexOptions.IgnoreCase);
        public static readonly Regex sentToGenesysRegex = new Regex(@"Sending\s<([\w\d.]+)>\smessage\sto\s<TServer>", RegexOptions.IgnoreCase);
        public static readonly Regex receivedFromGenesysRegex = new Regex(@"^Received\sGenesys\smessage:\s+([^\s]+)", RegexOptions.IgnoreCase);
        public static readonly Regex receivedFromFubuRegex = new Regex(@"^Received\s<([\w\d.]+)>\smessage\sfrom\s<lq\.tcp://[^>]+>", RegexOptions.IgnoreCase);
        public static readonly Regex receivedFromSscRegex = new Regex(@"^Received\s<([\w\d.]+)>\smessage\sfrom\s<SSC>", RegexOptions.IgnoreCase);
        public static readonly Regex payloadDetailsRegex = new Regex(@"^<([\w\d.]+)>\smessage\sdetails:", RegexOptions.IgnoreCase);
        public static readonly Regex timServiceRegex = new Regex(@"Sending\s<([\w\d/]+)>\srequest\sto\s<TIM\sservice>", RegexOptions.IgnoreCase);

        public static readonly List<Regex> specialInfoRegexes = new List<Regex>
        {
            new Regex(@"(^\(for\sWTWCallId\s=\s\d+,\sConnID\s=\s[\da-f]+\)$)", RegexOptions.IgnoreCase),
            new Regex(@"^(Unregistering\sCall\s\([\da-f]+\))\sas\slistener\sfor\sAcceptOfferMessage\.\.\.$", RegexOptions.IgnoreCase),
            new Regex(@"^(Adding\sparticipant\s'\d+',\slist\sis\snow\s\[[^\]]+])", RegexOptions.IgnoreCase),
            new Regex(@"^(Created\sconsultation\scall\sobject\s\([\da-f]+\))$", RegexOptions.IgnoreCase),
            new Regex(@"^(Waiting\sfor\sEventAttachedDataChanged\shaving\sdifferent\sRTargetAgentSelected,\sfound\schange\sfrom\s'.*'\sto\s'.*'.)$", RegexOptions.IgnoreCase)
        };

        public int LineNumber { get; }
        public DateTimeOffset LogDate { get; }
        public string LogLevel { get; }
        public string Namespace { get; }
        public string LogMessage { get; }
        public List<string> ExtraLines { get; } = new List<string>();
        public Entry PayloadEntry { get; set; }

        public Entry(int lineNumber, DateTimeOffset logDate, string logLevel, string @namespace, string message)
        {
            this.LineNumber = lineNumber;
            this.LogDate = logDate;
            this.LogLevel = logLevel;
            this.Namespace = @namespace;
            this.LogMessage = message;
        }

        public IEnumerable<string> Content => LogMessage.ToSingleton().Concat(ExtraLines.ToList());

        public bool IsSentToSsc => sentToSscRegex.IsMatch(LogMessage);
        public bool IsSentToFubu => sentToFubuRegex.IsMatch(LogMessage);
        public bool IsSentToGenesys => sentToGenesysRegex.IsMatch(LogMessage);
        public bool IsReceivedFromSsc => receivedFromSscRegex.IsMatch(LogMessage);
        public bool IsReceivedFromFubu => receivedFromFubuRegex.IsMatch(LogMessage);
        public bool IsReceivedFromGenesys => receivedFromGenesysRegex.IsMatch(LogMessage);

        public bool IsSentMessage => IsSentToSsc || IsSentToFubu || IsSentToGenesys || IsSentToTimService;
        public bool IsReceivedMessage => IsReceivedFromSsc || IsReceivedFromFubu || IsReceivedFromGenesys;

        public bool IsGenesysMessage => IsSentToGenesys || IsReceivedFromGenesys;
        public bool IsFubuMessage => IsSentToFubu || IsReceivedFromFubu;
        public bool IsSscMessage => IsSentToSsc || IsReceivedFromSsc;

        public bool IsMessage => IsSentMessage || IsReceivedMessage;
        public bool IsError => StringComparer.OrdinalIgnoreCase.Equals(LogLevel, "error");
        public bool IsWarning => StringComparer.OrdinalIgnoreCase.Equals(LogLevel, "warn");
        public bool IsSentToTimService => timServiceRegex.IsMatch(LogMessage);
        public bool IsSpecialInfo => specialInfoRegexes.Any(x => x.IsMatch(LogMessage));

        public string GetGenesysMessage()
        {
            var matchSent = sentToGenesysRegex.Match(LogMessage);
            var matchReceived = receivedFromGenesysRegex.Match(LogMessage);

            if (matchSent.Success)
                return matchSent.Groups[1].Value.RemoveTrailingData();
            else if (matchReceived.Success)
                return matchReceived.Groups[1].Value.RemoveTrailingData();
            else
                return null;
        }

        public string GetFubuMessage()
        {
            var matchSent = sentToFubuRegex.Match(LogMessage);
            var matchReceived = receivedFromFubuRegex.Match(LogMessage);

            if (matchSent.Success)
                return matchSent.Groups[1].Value;
            else if (matchReceived.Success)
                return matchReceived.Groups[1].Value;
            else
                return null;
        }

        public string GetSscMessage()
        {
            var matchSent = sentToSscRegex.Match(LogMessage);
            var matchReceived = receivedFromSscRegex.Match(LogMessage);

            if (matchSent.Success)
                return matchSent.Groups[1].Value;
            else if (matchReceived.Success)
                return matchReceived.Groups[1].Value;
            else
                return null;
        }

        public string GetMessageName()
        {
            if (IsGenesysMessage)
                return GetGenesysMessage();
            else if (IsFubuMessage)
                return GetFubuMessage();
            else if (IsSscMessage)
                return GetSscMessage();
            else
                return null;
        }

        public string GetTimServiceCall()
        {
            var match = timServiceRegex.Match(LogMessage);

            if (match.Success)
                return match.Groups[1].Value;
            else
                return null;
        }

        public string GetSpecialInfoText()
        {
            var matched = specialInfoRegexes.FirstOrDefault(x => x.IsMatch(LogMessage));

            if (matched == null)
                return null;

            var match = matched.Match(LogMessage);

            if (!match.Success)
                return null;

            return match.Groups[1].Value;
        }

        public static bool HasPayloadFor(Entry potentialPayload, Entry messageEntry)
        {
            if (messageEntry == null || potentialPayload == null)
                return false;

            var match = payloadDetailsRegex.Match(potentialPayload.LogMessage);

            if (!match.Success)
                return false;

            var payloadName = match.Groups[1].Value;

            if (messageEntry.IsGenesysMessage)
                payloadName = payloadName.RemoveTrailingData();

            return StringComparer.OrdinalIgnoreCase.Equals(messageEntry.GetMessageName(), payloadName);
        }
    }
}
