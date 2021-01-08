module OutputWriter

open System
open System.IO

type OutputWriter = {
    WriteAction: string -> unit
    WriteLineAction: string -> unit
    DoInColorContext: ConsoleColor -> (unit -> unit) -> unit
    SetWidth: int -> unit
    ResetCursorPosition: unit -> unit
    CanResetCursorPosition: bool
    CloseWriter: unit -> unit
}

let buildFileWriter (writer : StreamWriter) =
    { 
        WriteAction = fun text -> writer.Write(text)
        WriteLineAction = fun text -> writer.WriteLine(text)
        DoInColorContext = fun _ action -> action()
        SetWidth = fun _ -> ()
        ResetCursorPosition = fun () -> ()
        CanResetCursorPosition = false
        CloseWriter = 
            fun () ->
                writer.Flush()
                writer.Dispose()
    }


let buildConsoleWriter() =
    {
        WriteAction = Console.Write
        WriteLineAction = Console.WriteLine
        DoInColorContext = 
            fun color action ->
                let savedColor = Console.ForegroundColor

                Console.ForegroundColor <- color
                action()
                Console.ForegroundColor <- savedColor
        SetWidth = fun width -> Console.WindowWidth <- Math.Max(width, Console.WindowWidth)
        ResetCursorPosition = fun () -> Console.SetCursorPosition(0, Console.CursorTop)
        CanResetCursorPosition = true
        CloseWriter = fun () -> ()
    }
