module SummaryRenderer

open OutputWriter
open Entry
open System

let render outputWriter entries isHighlightedMessage =
    let write = outputWriter.WriteAction
    let writeLine = outputWriter.WriteLineAction
    
    let buildColoredWriter color action =
        fun text -> outputWriter.DoInColorContext (fun () -> action text) color

    let writeError = buildColoredWriter ConsoleColor.Red writeLine
    let writeWarning = buildColoredWriter ConsoleColor.Yellow writeLine
    let writeSpecialInfo = buildColoredWriter ConsoleColor.Cyan writeLine
    let writeEmphasized = buildColoredWriter ConsoleColor.Magenta write
    let writeMessageHighlight = buildColoredWriter ConsoleColor.Green write
    let writeSpaces num = write(String(' ', num))

    let safeMaxBy projection col = if Seq.isEmpty col then 0 else (Seq.map projection >> Seq.max) col

    let maxGenesysMessageNameLength = 
        let getLength entry = (getGenesysMessage entry) |> Option.fold (fun _ e -> e.Length) 0

        entries 
            |> Seq.filter (fun x -> (isReceivedFromGenesys x) && (isSentToGenesys x)) 
            |> safeMaxBy getLength

    let maxFubuMessageNameLength = 
        let getLength entry =
            match getFubuMessage entry with
            | Some m -> m.Length
            | None ->
                match getTimServiceCall entry with
                | Some m -> m.Length
                | None -> 0
        
        entries 
            |> Seq.filter (fun x -> (isReceivedFromFubu x) && (isSentToFubu x) && (isSentToTimService x))
            |> safeMaxBy getLength

    let maxSscMessageNameLength = 
        let getLength entry = (getSscMessage entry) |> Option.fold (fun _ e -> e.Length) 0

        entries 
            |> Seq.filter (fun x -> (isReceivedFromSsc x) && (isSentToSsc x))
            |> safeMaxBy getLength



    ()