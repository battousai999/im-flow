module SummaryRenderer

open OutputWriter
open Entry
open System
open System.IO

type RenderItem = 
    | FilenameSection of string
    | EntrySection of Entry

let render outputWriter entries isHighlightedMessage autoExpand areMultipleFiles =
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

    let dateFormat = "HH:mm:ss.ffff zzz"
    let lineNumberPadding = 
        let maxLineNumber = entries |> safeMaxBy (fun x -> x.LineNumber.ToString().Length)
        Math.Max(maxLineNumber, 7)
    let datePadding = DateTimeOffset.Now.ToString(dateFormat).Length
    let genesysPadding = Math.Max(maxGenesysMessageNameLength, 10)
    let interceptorPadding = 17
    let sscPadding = Math.Max(maxSscMessageNameLength, 10)
    let fubuPadding = Math.Max(maxFubuMessageNameLength, 13)
    let fullWidth = lineNumberPadding + 1 + datePadding + 3 + genesysPadding + interceptorPadding + sscPadding + 1 + fubuPadding + 1
    let nonGenesysInitialSpacing = String(' ', genesysPadding)
    let fubuAfterSpacing = String(' ', sscPadding + 1)

    let renderEntries = 
        if not areMultipleFiles then
            entries |> Seq.map (fun x -> EntrySection x)
        else
            let folder (currentList : RenderItem ResizeArray, currentFilename : string) entry =
                if StringComparer.OrdinalIgnoreCase.Equals(currentFilename, entry.Filename) then
                    currentList.Add(EntrySection entry)
                    (currentList, currentFilename)
                else
                    currentList.Add(FilenameSection entry.Filename)
                    currentList.Add(EntrySection entry)
                    (currentList, entry.Filename)
        
            let (resultEntries, _) = entries |> Seq.fold folder (ResizeArray(), null)

            resultEntries |> Seq.cast

    // Expand console width, if applicable
    if autoExpand then
        outputWriter.SetWidth (fullWidth + 1)

    // Write header
    write ("Line #".PadRight(lineNumberPadding + 1))
    write ("Date".PadRight(datePadding + 3))
    write ("Genesys".PadRight(genesysPadding))
    write "  (Interceptor)  "
    write ("SSC".PadRight(sscPadding))
    writeLine " CoreBus/TIM"
    writeLine (String('=', fullWidth))

    let renderEntry entry =
        match entry with
        | FilenameSection fullFilename ->
            let filename = Path.GetFileName(fullFilename)
            let bar = String('-', filename.Length)

            writeLine $"\n{bar}\n{filename}\n{bar}"
        | EntrySection entry ->
            

        ()

    renderEntries
        |> Seq.iter renderEntry

    ()