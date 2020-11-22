namespace b0wter.CouchDb.Lib.Databases

//
// Queries: /{db} [HEAD]
//

open b0wter.CouchDb.Lib
open b0wter.CouchDb.Lib.Core
open b0wter.FSharp

module Exists =

    type Result
        = Exists
        | DoesNotExist
        | DbNameMissing of RequestResult.TString
        | Unknown of RequestResult.TString

    let query (props: DbProperties.T) (name: string) : Async<Result> =
        async {
            if System.String.IsNullOrWhiteSpace(name) then return DbNameMissing <| RequestResult.create(None, "No query was sent to the server. You supplied an empty db name.") else
            let request = createHead props name []
            let! result = sendRequest request
            return match result.StatusCode with
                    | Some 200 -> Exists
                    | Some 404 -> DoesNotExist
                    | _ -> Unknown result
        }
        
    /// Returns the result from the query as a generic `FSharp.Core.Result`.
    let asResult (r: Result) =
        match r with
        | Exists -> Ok true
        | DoesNotExist -> Ok false
        | DbNameMissing e | Unknown e -> Error <| ErrorRequestResult.fromRequestResultAndCase(e, r)
        
    /// Runs query followed by asResult.
    let queryAsResult props name = query props name |> Async.map asResult