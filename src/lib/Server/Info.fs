namespace b0wter.CouchDb.Lib.Server

//
// Queries: /
//

open b0wter.CouchDb.Lib.Core
open b0wter.CouchDb.Lib
open Utilities

module Info =
    type VendorInfo = {
        Name: string
    }

    type Response = {
        Couchdb: string
        Uuid: System.Guid
        Vendor: VendorInfo
        Version: string
        [<Newtonsoft.Json.JsonProperty("git_sha")>]
        GitSha: string
        Features: string list
    }

    type Result
        = Success of Response
        | JsonDeserialisationError of RequestResult.StringRequestResult
        | Unknown of RequestResult.StringRequestResult

    let query (props: DbProperties.DbProperties) : Async<Result> =
        async {
            let request = createGet props "/" []
            let! result = sendTextRequest request
            return match result.StatusCode with
                    | Some 200 -> match deserializeJson<Response> result.Content with
                                  | Ok response -> Success response
                                  | Error e -> JsonDeserialisationError <| RequestResult.createForJson(e, result.StatusCode, result.Headers)
                    | _ -> Unknown result
        }
        
    /// Returns the result from the query as a generic `FSharp.Core.Result`.
    let asResult (r: Result) =
        match r with
        | Success response -> Ok response
        | JsonDeserialisationError e | Unknown e -> Error <| ErrorRequestResult.fromRequestResultAndCase(e, r)

    /// Runs query followed by asResult.
    let queryAsResult = query >> Async.map asResult
