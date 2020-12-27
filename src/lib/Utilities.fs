namespace b0wter.CouchDb.Lib

module Utilities =
    open Microsoft.FSharp.Reflection
    
    /// Retrieves the name of a union case.
    let getUnionCaseName (x:'a) = 
        match FSharpValue.GetUnionFields(x, typeof<'a>) with
        | case, _ -> case.Name  

    let switchListResult (results: Result<'a, 'b> list) : Result<'a list, 'b> =
        let rec step (acc: 'a list) (remaining: Result<'a, 'b> list) =
            match remaining with
            | (Ok a) :: tail -> step (a :: acc) tail
            | Error e :: _ -> Error e
            | [] -> Ok (acc |> List.rev)
        step [] results

    module UrlTemplate =

        type UrlTemplate1<'a> = {
            Parameter: 'a
            ParameterToString: 'a -> string
            Url: string
        }

        type UrlTemplate2<'a, 'b> = {
            Parameter1: 'a
            Parameter1ToString: 'a -> string
            Parameter2: 'b
            Parameter2ToString: 'a -> string
            Url: string
        }

        type UrlTemplate3<'a, 'b, 'c> = {
            Parameter1: 'a
            Parameter1ToString: 'a -> string
            Parameter2: 'b
            Parameter2ToString: 'b -> string
            Parameter3: 'c
            Parameter3ToString: 'c -> string
        }

        type UrlTemplate<'a, 'b, 'c>
            = SingleParameter of UrlTemplate1<'a>
            | TwoParameters of UrlTemplate2<'a, 'b>
            | ThreeParameters of UrlTemplate3<'a, 'b, 'c>

        let create1 (converter: 'a -> string) (url: string) (value: 'a) =
            SingleParameter { Parameter = value; ParameterToString = converter; Url = url }