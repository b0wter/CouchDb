namespace b0wter.CouchDb.Lib.Databases

//
// Queries: /{db} [GET]
//

open b0wter.CouchDb.Lib
open b0wter.CouchDb.Lib.Core
open b0wter.FSharp

module Infos =

    type Result
        = Success of Server.DbsInfo.Info
        | NotFound of RequestResult.T
        | JsonDeserializationError of RequestResult.T
        | DbNameMissing of RequestResult.T
        | Unknown of RequestResult.T

    /// Retrieves information of a single database.
    let query (props: DbProperties.T) (name: string) : Async<Result> =
        async {
            if System.String.IsNullOrWhiteSpace(name) then return DbNameMissing <| RequestResult.create(None, "No query was sent to the server. You supplied an empty db name.") else
            let request = createGet props name []
            let! result = sendRequest request
            return match result.statusCode with
                    | Some 200 -> match deserializeJson result.content with
                                    | Ok r -> Success r
                                    | Error e -> JsonDeserializationError <| RequestResult.createForJson(e, result.statusCode, result.headers)
                    | Some 404 -> NotFound result
                    | _ -> Unknown result
        }
    
    /// Returns the result from the query as a generic `FSharp.Core.Result`.
    let asResult (r: Result) =
        match r with
        | Success response -> Ok response
        | NotFound e | JsonDeserializationError e | DbNameMissing e | Unknown e -> Error e

    /// Runs query followed by asResult.
    let queryAsResult props name = query props name |> Async.map asResult