module Utils

open System
open System.IO
open System.Text.RegularExpressions

let containsWildcards (filename : string) =
    false


let enumerateFiles (filename : string) =
    let path = 
        if String.IsNullOrWhiteSpace(filename) then "" 
        else Path.GetDirectoryName(filename)

    let searchPattern = Path.GetFileName(filename)

    Seq.ofArray <| Directory.GetFiles(path, searchPattern)


let (|Regex|_|) (regex : Regex) input =
    let m = regex.Match(input)
    if m.Success then Some(List.tail [ for g in m.Groups -> g.Value ])
    else None


let removeTrailingData (text : string) =
    if text.EndsWith("data", StringComparison.OrdinalIgnoreCase)
    then text.Remove(text.Length - 4)
    else text
