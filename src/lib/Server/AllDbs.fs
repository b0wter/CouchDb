namespace b0wter.CouchDb.Lib.Server

//
// Queries: /_all_dbs
//

open b0wter.CouchDb.Lib.Core
open b0wter.CouchDb.Lib
open Utilities

module AllDbs =
    type Response = string list

    type Result
        = Success of Response
        | JsonDeserialisationError of RequestResult.StringRequestResult
        | Unknown of RequestResult.StringRequestResult

    /// <summary>
    /// Returns a list of strings containing the names of all databases.
    /// </summary>
    let query (props: DbProperties.DbProperties) : Async<Result> =
        async { 
            let request = createGet props "_all_dbs" []
            let! result = sendTextRequest request
            return match result.StatusCode with
                    | Some 200 -> match deserializeJson<Response> result.Content with
                                  | Ok response -> Success response
                                  | Error r -> JsonDeserialisationError <| RequestResult.createForJson(r, result.StatusCode, result.Headers)
                    | _ -> Unknown result
        }

    /// Returns the result from the query as a generic `FSharp.Core.Result`.
    let asResult (r: Result) =
        match r with
        | Success response -> Ok response
        | JsonDeserialisationError e | Unknown e -> Error <| ErrorRequestResult.fromRequestResultAndCase(e, r)
        
    /// Runs query followed by asResult.
    let queryAsResult = query >> Async.map asResult
    