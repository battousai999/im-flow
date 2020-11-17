﻿open System
open System.IO
open CommandLineParsing
open Utils
open OutputWriter
open im_flow
open type Battousai.Utils.ConsoleUtils

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