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
        ChangesDone: int
        Database: string
        Pid: string 
        Progress: int
        [<JsonProperty("started_on")>]
        StartedOn: uint64
        [<JsonProperty("total_changes")>]
        TotalChanges: int
        ``type``: string
        [<JsonProperty("updated_on")>]
        UpdatedOn: uint64       
    }

    type Response = Task list

    type Result
        = Success of Response
        | Unauthorized of RequestResult.TString
        | JsonDeserializationError of RequestResult.TString
        | Unknown of RequestResult.TString

    let query (props: DbProperties.T) : Async<Result> =
        async {
            let request = createGet props "_active_tasks" []
            let! result = sendTextRequest request
            return match result.StatusCode with
                    | Some 200 -> match deserializeJson result.Content with
                                  | Ok r -> Success r
                                  | Error e -> JsonDeserializationError <| RequestResult.createForJson(e, result.StatusCode, result.Headers)
                    | Some 401 -> Unauthorized result
                    | _ -> Unknown result
        }

    let asResult (r: Result) =
        match r with
        | Success s -> Ok s
        | Unauthorized e | JsonDeserializationError e | Result.Unknown e -> Error e

    let queryAsResult props = query props |> Async.map asResult