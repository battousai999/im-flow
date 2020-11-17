module CommandLineParsing

open System
open Fclp
open im_flow
open Fclp.Internals
open System.Linq

type ParseResults = 
    | Success of Args
    | ShowHelp of string


let createParser() =
    let parser = FluentCommandLineParser<Args>()

    parser.Setup(fun x -> x.Filenames)
        .As('i', "input-file")
        .Required()
        .UseForOrphanArguments()
        .WithDescription("<filename(s)>  The names of the files to process (required)")
        |> ignore

    parser.Setup(fun x -> x.DisableAutoExpandConsole)
        .As('x', "no-auto-expand-console")
        .SetDefault(false)
        .WithDescription("               Suppress expanding the width of the console to fit the content")
        |> ignore

    parser.Setup(fun x -> x.OutputFilename)
        .As('o', "output-file")
        .WithDescription("<filename>     Write output to a file with a given name")
        |> ignore

    parser.Setup(fun x -> x.OpenInEditor)
        .As('e', "open-in-editor")
        .SetDefault(false)
        .WithDescription("               Open the output in an editor")
        |> ignore

    parser.Setup(fun x -> x.IgnoreErrors)
        .As("ignore-errors")
        .SetDefault(false)
        .WithDescription("               Suppress display of errors in output")
        |> ignore

    parser.Setup(fun x -> x.SuppressAnnotations)
        .As('a', "suppress-annotations")
        .SetDefault(false)
        .WithDescription("               Suppress display of annotations in output")
        |> ignore

    parser.Setup(fun x -> x.IncludeHeartbeat)
        .As('h', "include-heartbeat")
        .SetDefault(false)
        .WithDescription("               Include Genesys heartbeat messages (EventAddressInfo) in output")
        |> ignore

    parser.Setup(fun x -> x.ParseLogDatesAsLocal)
        .As('l', "local-dates")
        .SetDefault(false)
        .WithDescription("               Parse log dates as local instead of UTC")
        |> ignore

    parser.Setup(fun x -> x.MatchMessages)
        .As('m', "match-messages")
        .WithDescription("               Highlight messages that match given names")
        |> ignore

    parser.Setup(fun x -> x.ShowAllInfoMessages)
        .As('f', "show-all-info")
        .SetDefault(false)
        .WithDescription("               Show all info messages")
        |> ignore

    parser.Setup(fun x -> x.ShowHelp)
        .As("help")
        .WithDescription("               Show this help information")
        |> ignore

    parser


let getHelpDisplay (parser : FluentCommandLineParser<'a>) =
    let longNamePadding = Enumerable.Max(parser.Options, fun x -> x.LongName.Length)
    let initialLines = 
        [
            "Invalid command-line parameters.";
            @"Example usage: .\im-flow.exe -i c:\some-folder\interceptor.log"
        ]

    let buildLine (option : ICommandLineOption) =
        let shortName = if option.HasShortName then $"-{option.ShortName}, " else "    "
        $"   {shortName}--{option.LongName.PadRight(longNamePadding)} {option.Description}"

    let lines = 
        List.map buildLine (List.ofSeq parser.Options)
            |> List.append initialLines

    List.fold (fun acc x -> $"{acc}{Environment.NewLine}{x}") "" lines


let parse args =
    let parser = createParser()
    let results = parser.Parse(args)
    
    if results.HasErrors || parser.Object.ShowHelp 
    then ShowHelp <| getHelpDisplay parser
    else Success parser.Object

