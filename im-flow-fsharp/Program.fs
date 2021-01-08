open System
open System.IO
open CommandLineParsing
open Utils
open OutputWriter
open EntryComparer
open im_flow
open type Battousai.Utils.ConsoleUtils
open System.Diagnostics

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
        (parameters.IncludeHeartbeat || (not <| StringComparer.OrdinalIgnoreCase.Equals(Option.defaultValue "" (Entry.getGenesysMessage x), "EventAddressInfo")))

    let filteredEntries = entries 
                            |> List.filter entriesFilter
                            |> List.sortWith entryComparer

    let outputWriter = if shouldOutputToConsole then buildConsoleWriter() else buildFileWriter(new StreamWriter(outputFilename, false))

    let isHighlightedMessage message = 
        let matchMessages = Option.defaultValue ([] : string list) <| Option.map List.ofSeq (Option.ofObj parameters.MatchMessages)

        if List.isEmpty matchMessages then
            false
        else
            matchMessages |> List.exists (fun x -> StringComparer.OrdinalIgnoreCase.Equals(x, message))

    SummaryRenderer.render
        outputWriter
        filteredEntries
        isHighlightedMessage
        (not parameters.DisableAutoExpandConsole)
        ((Seq.length allFilenames) > 1)
        parameters.SuppressAnnotations

    outputWriter.CloseWriter()

    if parameters.OpenInEditor then
        new Process(StartInfo = ProcessStartInfo(parameters.OutputFilename, UseShellExecute = true)) |> ignore


[<EntryPoint>]
let main argv =
    let parseArgs continuation = 
        let results = CommandLineParsing.parse argv

        match results with
        | ShowHelp text -> Log(text)
        | Success args -> continuation args

    RunLoggingExceptions (fun () -> parseArgs app)
    0 // return an integer exit code
