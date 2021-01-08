module EntryProcessing

open System
open System.IO
open System.Text.RegularExpressions
open System.Globalization
open Utils
open Entry
open System.Collections.Generic

let entryHeaderRegex = Regex(@"^\[(\d{4}-\d{2}-\d{2}\s\d{2}:\d{2}:\d{2}.\d{4})\s\|\s+(\w+)\]\s([\w\.\[\]`]+)\s-\s(.*)$")

type RawEntry = {
    Filename : string
    LineNumber : int
    Text : string
}


let readRawEntries (filenames : string seq) =
    let readFile filename =
        File.ReadAllLines(filename)
        |> Seq.mapi (fun i line -> { Filename = filename; LineNumber = i + 1; Text = line })

    filenames |> Seq.collect readFile


let associatePayloads (entries : Entry list) =
    let alreadyAssociatedEntries = HashSet<Entry>()

    let projection entry =
        let safeSkip num list = 
            list
                |> Seq.indexed
                |> Seq.skipWhile (fst >> (>) num)
                |> Seq.map snd

        let subsequentEntries = 
            entries 
                |> List.toSeq
                |> Seq.skipWhile (fun x -> x <> entry)
                |> safeSkip 1
                |> Seq.truncate 100

        let candidatePayloads = 
            seq {
                for item in subsequentEntries do
                    if not (alreadyAssociatedEntries.Contains(item)) then
                        yield item
            }

        let payload = candidatePayloads |> Seq.tryFind (hasPayloadFor entry)

        match payload with
        | Some p -> 
            alreadyAssociatedEntries.Add(p) |> ignore
            { entry with PayloadEntry = Some p }
        | None -> entry

    entries |> List.map projection


let parseEntries parseDatesAsLocal (rawEntries : RawEntry seq) =
    let dateTimeStyle = if parseDatesAsLocal then DateTimeStyles.AssumeLocal else DateTimeStyles.AssumeUniversal

    let accumulator (acc : Entry ResizeArray * Entry option * string ResizeArray) (rawEntry : RawEntry) =
        let (entries, lastEntry, extraLines) = acc

        match rawEntry.Text with
        | Utils.Regex entryHeaderRegex [ rawDate; logLevel; logNamespace; message ] ->
            let logDate = DateTimeOffset.Parse(rawDate, null, dateTimeStyle)

            let newEntry = 
                { 
                    Entry.Filename = rawEntry.Filename 
                    LineNumber = rawEntry.LineNumber
                    LogDate = logDate
                    LogLevel = logLevel
                    Namespace = logNamespace
                    LogMessage = message
                    ExtraLines = []
                    PayloadEntry = None
                }

            if lastEntry.IsSome then 
                entries.Add({ lastEntry.Value with ExtraLines = List.ofSeq extraLines })

            (entries, Some newEntry, ResizeArray())
        | _ -> 
            if lastEntry.IsNone then
                raise (InvalidOperationException("First line must contain a entry header."))

            extraLines.Add(rawEntry.Text)

            (entries, lastEntry, extraLines)

    let (entries, lastEntry, extraLines) = rawEntries |> Seq.fold accumulator (ResizeArray(), None, ResizeArray())

    if lastEntry.IsSome then
        entries.Add({ lastEntry.Value with ExtraLines = List.ofSeq extraLines })

    List.ofSeq entries |> associatePayloads
    