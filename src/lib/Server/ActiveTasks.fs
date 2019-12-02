namespace b0wter.CouchDb.Lib.Server

//
// Queries: /_active_tasks
//

open b0wter.CouchDb.Lib.Core
open b0wter.CouchDb.Lib
open b0wter.FSharp
open Newtonsoft.Json

module ActiveTasks =

    type Task = {
        [<JsonProperty("chandes_done")>]
        changesDone: int
        database: string
        pid: string 
        progress: int
        [<JsonProperty("started_on")>]
        startedOn: uint64
        [<JsonProperty("total_changes")>]
        totalChanges: int
        ``type``: string
        [<JsonProperty("updated_on")>]
        updatedOn: uint64       
    }

    type Response = Task list

    type Result
        = Success of Response
        | Unauthorized of RequestResult.T
        | JsonDeserializationError of RequestResult.T
        | Unknown of RequestResult.T

    let query (props: DbProperties.T) : Async<Result> =
        async {
            let request = createGet props "_active_tasks" []
            let! result = sendRequest request
            return match result.statusCode with
                    | Some 200 -> match deserializeJson result.content with
                                  | Ok r -> Success r
                                  | Error e -> JsonDeserializationError <| RequestResult.createForJson(e, result.statusCode, result.headers)
                    | Some 401 -> Unauthorized result
                    | _ -> Unknown result
        }

    let asResult (r: Result) =
        match r with
        | Success s -> Ok s
        | Unauthorized e | JsonDeserializationError e | Result.Unknown e -> Error e

    let queryAsResult props = query props |> Async.map asResult