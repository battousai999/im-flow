module Entry

open System
open System.Text.RegularExpressions
open Utils

let payloadDetailsRegex = Regex(@"^<([\w\d.]+)>\smessage\sdetails:", RegexOptions.IgnoreCase)
let sentToGenesysRegex = Regex(@"Sending\s<([\w\d.]+)>\smessage\sto\s<TServer>", RegexOptions.IgnoreCase)
let sentToFubuRegex = Regex(@"Sending\s<([\w\d.]+)>\smessage\sto\s<CoreBus>", RegexOptions.IgnoreCase)
let sentToSscRegex = Regex(@"Sending\s<([\w\d.]+)>\smessage\sto\s<SSC>", RegexOptions.IgnoreCase)
let receivedFromGenesysRegex = Regex(@"^Received\sGenesys\smessage:\s+([^\s]+)", RegexOptions.IgnoreCase)
let receivedFromFubuRegex = Regex(@"^Received\s<([\w\d.]+)>\smessage\sfrom\s<lq\.tcp://[^>]+>", RegexOptions.IgnoreCase)
let receivedFromSscRegex = Regex(@"^Received\s<([\w\d.]+)>\smessage\sfrom\s<SSC>", RegexOptions.IgnoreCase)

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


let isSentToGenesys (entry : Entry) = sentToGenesysRegex.IsMatch(entry.LogMessage)
let isSentToFubu (entry : Entry) = sentToFubuRegex.IsMatch(entry.LogMessage)
let isSentToSsc (entry : Entry) = sentToSscRegex.IsMatch(entry.LogMessage)

let isReceivedFromGenesys (entry : Entry) = receivedFromGenesysRegex.IsMatch(entry.LogMessage)
let isReceivedFromFubu (entry : Entry) = receivedFromFubuRegex.IsMatch(entry.LogMessage)
let isReceivedFromSsc (entry : Entry) = receivedFromSscRegex.IsMatch(entry.LogMessage)

let isGenesysMessage entry = (isSentToGenesys entry) || (isReceivedFromGenesys entry)
let isFubuMessage entry = (isSentToFubu entry) || (isReceivedFromFubu entry)
let isSscMessage entry = (isSentToSsc entry) || (isReceivedFromSsc entry)


let getGenesysMessage (entry : Entry) =
    match entry.LogMessage with
    | Utils.Regex sentToGenesysRegex [ messageName ] ->
        Utils.removeTrailingData messageName
    | _ -> 
        match entry.LogMessage with
        | Utils.Regex receivedFromGenesysRegex [ messageName ] ->
            Utils.removeTrailingData messageName
        | _ -> null


let getFubuMessage (entry : Entry) =
    match entry.LogMessage with
    | Utils.Regex sentToFubuRegex [ messageName ] -> messageName
    | _ -> 
        match entry.LogMessage with
        | Utils.Regex receivedFromFubuRegex [ messageName ] -> messageName
        | _ -> null


let getSscMessage (entry : Entry) =
    match entry.LogMessage with
    | Utils.Regex sentToSscRegex [ messageName ] -> messageName
    | _ -> 
        match entry.LogMessage with
        | Utils.Regex receivedFromSscRegex [ messageName ] -> messageName
        | _ -> null


let getMessageName entry =
    if isGenesysMessage entry then getGenesysMessage entry
    else if isFubuMessage entry then getFubuMessage entry
    else if isSscMessage entry then getSscMessage entry
    else null


let hasPayloadFor (sourceEntry : Entry) (candidate : Entry) =
    match candidate.LogMessage with
    | Utils.Regex payloadDetailsRegex [ rawPayloadName ] ->
        let payloadName = 
            if isGenesysMessage sourceEntry
            then Utils.removeTrailingData rawPayloadName
            else rawPayloadName

        StringComparer.OrdinalIgnoreCase.Equals(getMessageName sourceEntry, payloadName)
    | _ -> false
