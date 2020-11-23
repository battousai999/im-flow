open System
open System.IO
open CommandLineParsing
open Utils
open OutputWriter
open im_flow
open type Battousai.Utils.ConsoleUtils
open System.Collections.Generic

let app (parameters : Args) =
    let expandFiles filename =
        if containsWildcards filename then enumerateFiles filename
        else Seq.singleton filename

    let allFilenames = parameters.Filenames |> Seq.collect expandFiles
    let shouldOutputToConsole = String.IsNullOrWhiteSpace(parameters.OutputFilename)

    let outputFilename = 
        if parameters.OpenInEditor && String.IsNullOrWhiteSpace(parameters.OutputFilename) 
        then $"{Path.GetTempFileName()}.txt"
        else parameters.OutputFilename            

    let rawEntries = EntryProcessing.readRawEntries allFilenames
    let entries = rawEntries |> EntryProcessing.parseEntries parameters.ParseLogDatesAsLocal

    let isError = fun x -> (not parameters.IgnoreErrors) && ((Entry.isError x) || (Entry.isFatal x) || (Entry.isWarning x))
    let isFullInfo = fun x -> parameters.ShowAllInfoMessages && (Entry.isNonMessageInfo x)

    let entriesFilter = fun x ->
        ((Entry.isMessage x) || (isError x) || (Entry.isSpecialInfo x) || (isFullInfo x)) &&
        (parameters.IncludeHeartbeat || (not <| StringComparer.OrdinalIgnoreCase.Equals((Entry.getGenesysMessage x), "EventAddressInfo")))

    let entriesComparer = fun (x : Entry.Entry) (y : Entry.Entry) ->
        let dateComparer (x : Entry.Entry) (y : Entry.Entry) = Comparer<DateTimeOffset>.Default.Compare(x.LogDate, y.LogDate)
        let secondaryComparer (x : Entry.Entry) (y : Entry.Entry) = 
            let intComparer = Comparer<int>.Default.Compare

            if StringComparer.OrdinalIgnoreCase.Equals(x.Filename, y.Filename)
            then intComparer(x.LineNumber, y.LineNumber)
            else -intComparer(x.LineNumber, y.LineNumber)

        match (dateComparer x y) with
        | value when value <> 0 -> value
        | _ -> secondaryComparer x y

    let filteredEntries = entries 
                            |> List.filter entriesFilter
                            |> List.sortWith entriesComparer

    use streamWriter = if shouldOutputToConsole then null else new StreamWriter(outputFilename, false)
    let outputWriter = if shouldOutputToConsole then buildConsoleWriter() else buildFileWriter(streamWriter)

    ()

[<EntryPoint>]
let main argv =
    let parseArgs continuation = 
        let results = CommandLineParsing.parse argv

        match results with
        | ShowHelp text -> Log(text)
        | Success args -> continuation args

    RunLoggingExceptions (fun () -> parseArgs app)
    0 // return an integer exit code
