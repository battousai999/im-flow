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

    Directory.GetFiles(path, searchPattern) |> Seq.ofArray


let (|Regex|_|) (regex : Regex) input =
    let m = regex.Match(input)

    if m.Success then Some(List.tail [ for g in m.Groups -> g.Value ])
    else None


let removeTrailingData (text : string) =
    if text.EndsWith("data", StringComparison.OrdinalIgnoreCase)
    then text.Remove(text.Length - 4)
    else text


let formatPhoneNumber text =
    if String.IsNullOrWhiteSpace(text) then
        String.Empty
    else
        let isAllDigits = text |> Seq.forall (fun x -> Char.IsDigit x)

        if isAllDigits && text.Length = 10 then
            $"{text.Substring(0, 3)}-{text.Substring(3, 3)}-{text.Substring(6)}"
        else
            text


let formatWithHeader header text =
    if String.IsNullOrWhiteSpace(text) then
        String.Empty
    else
        $"{header}{text}"

