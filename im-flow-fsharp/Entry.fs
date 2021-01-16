module Entry

open System
open System.Text.RegularExpressions
open Utils

let payloadDetailsRegex = Regex(@"^<([\w\d.]+)>\smessage\sdetails:", RegexOptions.IgnoreCase)
let sentToGenesysRegex = Regex(@"Sending\s<([\w\d.]+)>\smessage\sto\s<TServer>", RegexOptions.IgnoreCase)
let sentToFubuRegex = Regex(@"Sending\s<([\w\d.]+)>\smessage\sto\s<CoreBus>", RegexOptions.IgnoreCase)
let sentToSscRegex = Regex(@"Sending\s<([\w\d.]+)>\smessage\sto\s<SSC>", RegexOptions.IgnoreCase)
let sentToTimServiceRegex = Regex(@"Sending\s<([\w\d/]+)>\srequest\sto\s<TIM\sservice>", RegexOptions.IgnoreCase)
let receivedFromGenesysRegex = Regex(@"^Received\sGenesys\smessage:\s+([^\s]+)", RegexOptions.IgnoreCase)
let receivedFromFubuRegex = Regex(@"^Received\s<([\w\d.]+)>\smessage\sfrom\s<lq\.tcp://[^>]+>", RegexOptions.IgnoreCase)
let receivedFromSscRegex = Regex(@"^Received\s<([\w\d.]+)>\smessage\sfrom\s<SSC>", RegexOptions.IgnoreCase)

let specialInfoRegexes = [
    Regex(@"^(\(for\sWTWCallId\s=\s\d+,\sConnID\s=\s[\da-f]+\))$", RegexOptions.IgnoreCase)
    Regex(@"^(\(Initialize\soutbound\scall\sfor\sWTWCallId\s=\s\d+,\sConnID\s=\s[\da-f]+\))$", RegexOptions.IgnoreCase)
    Regex(@"^(Unregistering\sCall\s\([\da-f]+\))\sas\slistener\sfor\sAcceptOfferMessage\.\.\.$", RegexOptions.IgnoreCase)
    Regex(@"^(Adding\sparticipant\s'[^']*',\slist\sis\snow\s\[[^\]]+])", RegexOptions.IgnoreCase)
    Regex(@"^(Removing\sparticipant\s'[^']*',\slist\sis\snow\s\[[^\]]*\])", RegexOptions.IgnoreCase)
    Regex(@"^(Setting\sparticipants\sfrom\sattached\sdata,\slist\sis\snow\s\[[^\]]+])", RegexOptions.IgnoreCase)
    Regex(@"^(Created\sconsultation\scall\sobject\s\([\da-f]+\))$", RegexOptions.IgnoreCase)
    Regex(@"^(Waiting\sfor\sEventAttachedDataChanged\shaving\sdifferent\sRTargetAgentSelected,\sfound\schange\sfrom\s'.*'\sto\s'.*'.)$", RegexOptions.IgnoreCase)
    Regex(@"^(Interceptor\sversion:\s+.*)$", RegexOptions.IgnoreCase)
    Regex(@"^(Registering\sGenesys\saddress\s\(.+\))\.\.\.$", RegexOptions.IgnoreCase)
    Regex(@"^(Retrieving\suser-specific\ssettings\s\(for\s'[^']*'\))\.\.\.", RegexOptions.IgnoreCase)
    Regex(@"^(Using\sLocal\sURI\s\(lq.tcp://[^/]*/interceptor\))", RegexOptions.IgnoreCase)
    Regex(@"^(Channel\s\w+\son\s\w+\sendpoint\s\([^)]*\))", RegexOptions.IgnoreCase)
    Regex(@"^(Set\senvironment\sspecific\sskill:\ss\.Env\.[\w\d]+)", RegexOptions.IgnoreCase)
    Regex(@"^(Setting\sAgentSipUri\sto:\s.+)$", RegexOptions.IgnoreCase)
    Regex(@"^(Setting\sAgentEmployeeId\sto:\s\d+)", RegexOptions.IgnoreCase)
    Regex(@"^(Unfinished\scall\sfile\sfound\s-\sattempting\sto\srecreate\scall\sobjects.)", RegexOptions.IgnoreCase)
    Regex(@"^(Genesys\sreports\sno\sunfinished\scalls)", RegexOptions.IgnoreCase)
    Regex(@"^(Unfinished\scall\s[\da-f]+\srecreated)", RegexOptions.IgnoreCase)
    Regex(@"^(Swapping\sheld\scalls\sActive:\s[\da-f]+\sHeld:\s[\da-f]+)", RegexOptions.IgnoreCase)
    Regex(@"^(Deferring\sremoval\sof\sparticipant\s\(DN=[^)]+\)\suntil\scall\sinvitation\saccepted)", RegexOptions.IgnoreCase)
    Regex(@"^(Connection\sID\schanged\sfrom\s[\da-f]+\sto\s[\da-f]+)", RegexOptions.IgnoreCase)
    Regex(@"^(Assign\snew\sCallEndpointNumber\s\([^)]+\)\sfrom\sparticipant\sserialization\sfor\s[^/]*/.*)$", RegexOptions.IgnoreCase)
    Regex(@"^(>>>.*)$")
]

let emphasizedMessages = [
    "RequestMakeCall"
    "EventRinging"
    "OfferCallbackMessage"
]

let ignoredInfoRegexes = [
    Regex(@"Passing through message to", RegexOptions.IgnoreCase)
]

type AnnotationInfo = {
    Regexes: Regex list
    Projection: string list -> string
}

let annotations = 
    [
        "RequestMakeCall", 
        { 
            Regexes = [Regex("\"WTW_DNIS\":\\s\"([^\"]+)\",")]
            Projection = fun matches -> $"({Utils.formatPhoneNumber matches.[0]})"
        }

        "EventRinging",
        {
            Regexes = [Regex("\\.OtherDN\":\\s\"([^\"]+)\",")]
            Projection = fun matches -> $"({Utils.formatPhoneNumber matches.[0]})"
        }

        "EmployeePresenceChangingMessage",
        {
            Regexes = [Regex("\"Availability\":\\s\"([^\"]+)\",")]
            Projection = fun matches -> $"({matches.[0]})"
        }

        "EmployeePresenceChangedMessage",
        {
            Regexes = [Regex("\"Availability\":\\s\"([^\"]+)\",")]
            Projection = fun matches -> $"({matches.[0]})"
        }

        "EventReleased",
        {
            Regexes = [Regex("\\.ConnID\":\\s\"([^\"]+)\",")]
            Projection = fun matches -> $"({matches.[0]})"
        }

        "ParticipantChangedMessage",
        {
            Regexes = [
                Regex("\"Uri\":\\s\"([^\"]*)\"")
                Regex("\"State\":\\s\"([^\"]*)\",")
            ]
            Projection = fun matches -> $"({matches.[0]}/{matches.[1]})"
        }

        "DispositionCallMessage",
        {
            Regexes = [Regex("\"DispositionId\":\\s(\\d+),")]
            Projection = fun matches -> $"(DispositionId: {matches.[0]})"
        }

        "InviteInternalParticipantMessage",
        {
            Regexes = [
                Regex("\"InvitedSipAddress\":\\s\"([^\"]+)\",")
                Regex("\"IsTwoStep\":\\s([^,]+),")
            ]
            Projection = fun matches -> 
                let formatWithHeader = Utils.formatWithHeader ", IsTwoStep: "
                $"({matches.[0]}{formatWithHeader matches.[1]})"
        }

        "AddExternalParticipantMessage",
        {
            Regexes = [
                Regex("\"PhoneNumber\":\\s\"([^\"]+)\"")
                Regex("\"IsTwoStep\":\\s([^,]+),")
            ]
            Projection = fun matches ->
                let formatWithHeader = Utils.formatWithHeader ", IsTwoStep: "
                $"({Utils.formatPhoneNumber matches.[0]}{formatWithHeader matches.[1]})"
        }

        "TransferCallToEmployeeMessage",
        {
            Regexes = [
                Regex("\"DesiredRoleId\":\\s([^,]+),")
                Regex("\"IsWarmTransfer\":\\s([^,]+),")
                Regex("\"IsTwoStep\":\\s([^,]+),")
            ]
            Projection = fun matches ->
                let format1 = Utils.formatWithHeader ", IsWarmTransfer: "
                let format2 = Utils.formatWithHeader ", IsTwoStep: "
                $"(DesiredRoleId: {matches.[0]}{format1 matches.[1]}{format2 matches.[2]})"
        }

        "TserverReconnectionStatusChangedMessage",
        {
            Regexes = [Regex("\"Status\":\\s\"([^\"]+)\"")]
            Projection = fun matches -> $"({matches.[0]})"
        }

        "EventError",
        {
            Regexes = [
                Regex("\\.ErrorCode\":\\s(\\d+),")
                Regex("\\.ErrorMessage\":\\s\"([^\"]*)\",")
            ]
            Projection = fun matches -> $"({matches.[0]} — {matches.[1]})"
        }
    ] |> Map.ofList

type Entry = {
    Filename : string
    LineNumber : int
    LogDate : DateTimeOffset
    LogLevel : string
    Namespace : string
    LogMessage : string
    ExtraLines : string list
    PayloadEntry : Entry option
}


let isSentToGenesys entry = sentToGenesysRegex.IsMatch(entry.LogMessage)
let isSentToFubu entry = sentToFubuRegex.IsMatch(entry.LogMessage)
let isSentToSsc entry = sentToSscRegex.IsMatch(entry.LogMessage)
let isSentToTimService entry = sentToTimServiceRegex.IsMatch(entry.LogMessage)

let isReceivedFromGenesys entry = receivedFromGenesysRegex.IsMatch(entry.LogMessage)
let isReceivedFromFubu entry = receivedFromFubuRegex.IsMatch(entry.LogMessage)
let isReceivedFromSsc entry = receivedFromSscRegex.IsMatch(entry.LogMessage)

let isSentMessage entry = (isSentToGenesys entry) || (isSentToFubu entry) || (isSentToSsc entry) || (isSentToTimService entry)
let isReceivedMessage entry = (isReceivedFromGenesys entry) || (isReceivedFromFubu entry) || (isReceivedFromSsc entry)
let isMessage entry = (isSentMessage entry) || (isReceivedMessage entry)

let isGenesysMessage entry = (isSentToGenesys entry) || (isReceivedFromGenesys entry)
let isFubuMessage entry = (isSentToFubu entry) || (isReceivedFromFubu entry)
let isSscMessage entry = (isSentToSsc entry) || (isReceivedFromSsc entry)

let isSpecialInfo entry = specialInfoRegexes |> List.exists (fun (x : Regex) -> x.IsMatch(entry.LogMessage))
let isIgnoredInfo entry = ignoredInfoRegexes |> List.exists (fun (x : Regex) -> x.IsMatch(entry.LogMessage))

let isNonMessageInfo entry = 
    StringComparer.OrdinalIgnoreCase.Equals(entry.LogLevel, "info") && 
    (not << isMessage <| entry) && 
    (not << isIgnoredInfo <| entry)

let isIgnoredWarning entry = StringComparer.OrdinalIgnoreCase.Equals(entry.LogMessage, "setting name 'Interceptor' is invalid")

let isError entry = StringComparer.OrdinalIgnoreCase.Equals(entry.LogLevel, "error")
let isFatal entry = StringComparer.OrdinalIgnoreCase.Equals(entry.LogLevel, "fatal")
let isWarning entry = StringComparer.OrdinalIgnoreCase.Equals(entry.LogLevel, "warn") && (not << isIgnoredWarning <| entry)


let getGenesysMessage (entry : Entry) =
    match entry.LogMessage with
    | Utils.Regex sentToGenesysRegex [ messageName ] ->
        Some (Utils.removeTrailingData messageName)
    | _ -> 
        match entry.LogMessage with
        | Utils.Regex receivedFromGenesysRegex [ messageName ] ->
            Some (Utils.removeTrailingData messageName)
        | _ -> None


let getFubuMessage (entry : Entry) =
    match entry.LogMessage with
    | Utils.Regex sentToFubuRegex [ messageName ] -> Some messageName
    | _ -> 
        match entry.LogMessage with
        | Utils.Regex receivedFromFubuRegex [ messageName ] -> Some messageName
        | _ -> None


let getSscMessage (entry : Entry) =
    match entry.LogMessage with
    | Utils.Regex sentToSscRegex [ messageName ] -> Some messageName
    | _ -> 
        match entry.LogMessage with
        | Utils.Regex receivedFromSscRegex [ messageName ] -> Some messageName
        | _ -> None


let getTimServiceCall (entry : Entry) =
    match entry.LogMessage with
    | Utils.Regex sentToTimServiceRegex [ messageName ] -> Some messageName
    | _ -> None


let getMessageName entry =
    if isGenesysMessage entry then getGenesysMessage entry
    else if isFubuMessage entry then getFubuMessage entry
    else if isSscMessage entry then getSscMessage entry
    else None


let hasPayloadFor (sourceEntry : Entry) (candidate : Entry) =
    match getMessageName sourceEntry with
    | Some messageName ->
        match candidate.LogMessage with
        | Utils.Regex payloadDetailsRegex [ rawPayloadName ] ->
            let payloadName = 
                if isGenesysMessage sourceEntry
                then Utils.removeTrailingData rawPayloadName
                else rawPayloadName

            StringComparer.OrdinalIgnoreCase.Equals(messageName, payloadName)
        | _ -> false
    | None -> false


let errorAnnotation entry =
    if not ((isError entry) || (isFatal entry)) || (List.isEmpty entry.ExtraLines) then
        String.Empty
    else
        List.head entry.ExtraLines


let hasErrorAnnotation entry = not (String.IsNullOrWhiteSpace(errorAnnotation entry))


let getSpecialInfoText entry = 
    let matched = specialInfoRegexes |> List.tryFind (fun x -> x.IsMatch(entry.LogMessage))

    match matched with
    | Some m -> 
        let results = m.Match(entry.LogMessage)
        if results.Success then
            results.Groups.[1].Value
        else
            String.Empty
    | None -> String.Empty


let isEmphasizedMessage entry = 
    let messageName = getMessageName entry

    let equals x =
        match messageName with
        | Some m -> StringComparer.OrdinalIgnoreCase.Equals(m, x)
        | None -> false

    emphasizedMessages |> List.exists equals


let calculateAnnotation annotation (lines : string seq) =
    let getAnnotation text =
        let matches = annotation.Regexes |> List.map (fun x -> x.Match(text))

        if not (matches |> List.exists (fun x -> x.Success)) then
            String.Empty
        else
            let values = matches |> List.map (fun x -> if x.Success then x.Groups.[1].Value else String.Empty)

            annotation.Projection values

    getAnnotation <| String.Join(Environment.NewLine, lines)


let getAnnotation entry =
    let extraLines = match entry.PayloadEntry with | Some p -> p.ExtraLines | None -> []

    if List.isEmpty extraLines then
        String.Empty
    else
        let annotation = Map.tryFind (Option.defaultValue String.Empty (getMessageName entry)) annotations

        match annotation with
        | Some a -> calculateAnnotation a extraLines
        | None -> String.Empty


let hasAnnotation entry = not (String.IsNullOrWhiteSpace(getAnnotation entry))
