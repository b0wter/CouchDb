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
            
    module String =
        let join delimiter (strings: seq<string>) : string =
            System.String.Join(delimiter, strings)
            
        let isNullOrWhiteSpace (s: string) : bool =
            System.String.IsNullOrWhiteSpace(s)
            
    module Async =
        /// <summary>
        /// Performs a map operation on the result of an Async.
        /// </summary>
        let map f operation = async {
            let! x = operation
            let value = f x 
            return value
        }
        
    module Result =
        /// <summary>
        /// Takes a mapping for a value and an error an applies it based on the contents of the result.
        /// E.g.: ifValue is applied if the result is `Ok o` and ifError is applied if the result is `Error e`. 
        /// The return value will still be wrapped in a Result-Type!
        /// </summary>
        let mapBoth (ifValue: 'value -> 'newValue) (ifError: 'error -> 'newError) (result: Result<'value, 'error>) : Result<'newValue, 'newError> =
            match result with
            | Ok o -> Ok (o |> ifValue)
            | Error e -> Error (e |> ifError)        