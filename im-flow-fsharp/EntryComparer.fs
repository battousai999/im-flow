module EntryComparer

open System
open System.Collections.Generic
open Entry

let entryComparer (x : Entry) (y : Entry) =
    let dateComparer (x : Entry) (y : Entry) = Comparer<DateTimeOffset>.Default.Compare(x.LogDate, y.LogDate)
    let secondaryComparer (x : Entry) (y : Entry) = 
        let intComparer = Comparer<int>.Default.Compare

        if StringComparer.OrdinalIgnoreCase.Equals(x.Filename, y.Filename)
        then intComparer(x.LineNumber, y.LineNumber)
        else -intComparer(x.LineNumber, y.LineNumber)

    match (dateComparer x y) with
    | value when value <> 0 -> value
    | _ -> secondaryComparer x y