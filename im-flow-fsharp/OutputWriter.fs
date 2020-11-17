module OutputWriter

open System
open System.IO

type OutputWriter = {
    WriteAction: string -> unit
    WriteLineAction: string -> unit
    DoInColorContext: (unit -> unit) -> ConsoleColor -> unit
}

let buildFileWriter (writer : StreamWriter) =
    { 
        WriteAction = fun text -> writer.Write(text)
        WriteLineAction = fun text -> writer.WriteLine(text)
        DoInColorContext = fun action _ -> action()
    }


let buildConsoleWriter() =
    {
        WriteAction = Console.Write
        WriteLineAction = Console.WriteLine
        DoInColorContext = 
            fun action color ->
                let savedColor = Console.ForegroundColor

                Console.ForegroundColor <- color
                action()
                Console.ForegroundColor <- savedColor
    }
