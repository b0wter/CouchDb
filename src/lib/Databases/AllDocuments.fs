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
        | Failure of ErrorRequestResult

    type KeyCollection = {
        keys: string list
    }

    let private query (request: unit -> Async<FSharp.Data.HttpResponse>) =
        async {
            let! result = (sendRequest request) |> Async.map (fun x -> x :> IRequestResult)
            match result.StatusCode with
                | Some 200 -> 
                        match deserializeJson<Response> [] result.Body with
                        | Ok r -> return Success r
                        | Error e -> return Failure <| errorRequestResult (result.StatusCode, sprintf "Error: %s %s JSON: %s" e.reason System.Environment.NewLine e.json, Some result.Headers)
                | _ -> return Failure <| errorRequestResult (result.StatusCode, result.Body, Some result.Headers)
        }

    let queryAll (props: DbProperties.T) (dbName: string) : Async<Result> =
        let request = createGet props (sprintf "%s/_all_docs" dbName) []
        query request

    let querySelected (props: DbProperties.T) (dbName: string) (keys: string list) : Async<Result> =
        let keyCollection = { keys = keys }
        let request = createJsonPost props (sprintf "%s/_all_docs" dbName) keyCollection []
        query request



