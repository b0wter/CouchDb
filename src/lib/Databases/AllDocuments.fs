namespace b0wter.CouchDb.Lib.Database

//
// Queries: /{db}/_all_docs
//

open b0wter.CouchDb.Lib
open b0wter.CouchDb.Lib.Core
open b0wter.FSharp

module AllDocuments =
    type ResponseRowValue = {
        rev: string
    }
    type ResponseRow = {
        id: System.Guid
        key: string
        value: ResponseRowValue option
    }
    type Response = {
        offset: int option
        rows: ResponseRow []
        total_rows: int
    }

    type Result
        = Success of Response
        | BadRequest of ErrorRequestResult
        | Unauthorized of ErrorRequestResult
        | NotFound of ErrorRequestResult
        | JsonDeserialisationError of ErrorRequestResult
        | Unknown of ErrorRequestResult

    type KeyCollection = {
        keys: string list
    }

    let private query (request: unit -> Async<FSharp.Data.HttpResponse>) =
        async {
            let! result = (sendRequest request) |> Async.map (fun x -> x :> IRequestResult)
            return match result.StatusCode with
                   | Some 200 -> 
                        match deserializeJson<Response> [] result.Body with
                        | Ok r    -> Success r
                        | Error e -> JsonDeserialisationError <| errorRequestResult (result.StatusCode, sprintf "Error: %s %s JSON: %s" e.reason System.Environment.NewLine e.json, Some result.Headers)
                   | Some 400     -> BadRequest <| errorFromIRequestResult result
                   | Some 401     -> Unauthorized <| errorFromIRequestResult result
                   | Some 404     -> NotFound <| errorFromIRequestResult result
                   | _            -> Unknown <| errorFromIRequestResult result
        }

    let queryAll (props: DbProperties.T) (dbName: string) : Async<Result> =
        let request = createGet props (sprintf "%s/_all_docs" dbName) []
        query request

    let querySelected (props: DbProperties.T) (dbName: string) (keys: string list) : Async<Result> =
        let keyCollection = { keys = keys }
        let request = createJsonPost props (sprintf "%s/_all_docs" dbName) keyCollection []
        query request



