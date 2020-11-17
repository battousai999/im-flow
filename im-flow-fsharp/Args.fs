namespace im_flow

type Args() =
    let mutable filenames = System.Collections.Generic.List<string>()
    let mutable disableAutoExpandConsole = false
    let mutable outputFilename = ""
    let mutable openInEditor = false
    let mutable ignoreErrors = false
    let mutable suppressAnnotations = false
    let mutable includeHeartbeat = false
    let mutable parseLogDatesAsLocal = false
    let mutable matchMessages = System.Collections.Generic.List<string>()
    let mutable showHelp = false
    let mutable showAllInfoMessages = false

    member this.Filenames 
        with get() = filenames
        and set(value) = filenames <- value

    member this.DisableAutoExpandConsole
        with get() = disableAutoExpandConsole
        and set(value) = disableAutoExpandConsole <- value

    member this.OutputFilename
        with get() = outputFilename
        and set(value) = outputFilename <- value

    member this.OpenInEditor
        with get() = openInEditor
        and set(value) = openInEditor <- value

    member this.IgnoreErrors
        with get() = ignoreErrors
        and set(value) = ignoreErrors <- value

    member this.SuppressAnnotations
        with get() = suppressAnnotations
        and set(value) = suppressAnnotations <- value

    member this.IncludeHeartbeat
        with get() = includeHeartbeat
        and set(value) = includeHeartbeat <- value

    member this.ParseLogDatesAsLocal
        with get() = parseLogDatesAsLocal
        and set(value) = parseLogDatesAsLocal <- value

    member this.MatchMessages
        with get() = matchMessages
        and set(value) = matchMessages <- value

    member this.ShowHelp
        with get() = showHelp
        and set(value) = showHelp <- value

    member this.ShowAllInfoMessages
        with get() = showAllInfoMessages
        and set(value) = showAllInfoMessages <- value
