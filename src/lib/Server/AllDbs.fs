namespace b0wter.CouchDb.Lib.Server

//
// Queries: /_all_dbs
//

open b0wter.CouchDb.Lib.Core
open b0wter.CouchDb.Lib
open b0wter.FSharp

module AllDbs =
    type Response = string list

    type Result
        = Success of Response
        | JsonDeserialisationError of RequestResult.T
        | Unknown of RequestResult.T

    /// <summary>
    /// Returns a list of strings containing the names of all databases.
    /// </summary>
    let query (props: DbProperties.T) : Async<Result> =
        async { 
            let request = createGet props "_all_dbs" []
            let! result = sendRequest request
            return match result.statusCode with
                    | Some 200 -> match deserializeJson<Response> result.content with
                                  | Ok response -> Success response
                                  | Error r -> JsonDeserialisationError <| RequestResult.createForJson(r, result.statusCode, result.headers)
                    | _ -> Unknown result
        }

    /// Returns the result from the query as a generic `FSharp.Core.Result`.
    let asResult (r: Result) =
        match r with
        | Success response -> Ok response
        | JsonDeserialisationError e | Unknown e -> Error <| ErrorRequestResult.fromRequestResultAndCase(e, r)
        
    let queryAsResult = query >> Async.map asResult
    
