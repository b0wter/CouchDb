namespace b0wter.CouchDb.Lib.Server

//
// Queries: /
//

open b0wter.CouchDb.Lib.Core
open b0wter.CouchDb.Lib
open b0wter.FSharp

module Info =
    type VendorInfo = {
        name: string
    }

    type Response = {
        couchdb: string
        uuid: System.Guid
        vendor: VendorInfo
        version: string
        git_sha: string
        features: string list
    }

    type Result
        = Success of Response
        | JsonDeserialisationError of RequestResult.T
        | Unknown of RequestResult.T

    let query (props: DbProperties.T) : Async<Result> =
        async {
            let request = createGet props "/" []
            let! result = sendRequest request
            return match result.statusCode with
                    | Some 200 -> match deserializeJson<Response> result.content with
                                  | Ok response -> Success response
                                  | Error e -> JsonDeserialisationError <| RequestResult.createForJson(e, result.statusCode, result.headers)
                    | _ -> Unknown result
        }
        
    /// Returns the result from the query as a generic `FSharp.Core.Result`.
    let asResult (r: Result) =
        match r with
        | Success response -> Ok response
        | JsonDeserialisationError e | Unknown e -> Error <| ErrorRequestResult.fromRequestResultAndCase(e, r)

    let queryAsResult = query >> Async.map asResult
