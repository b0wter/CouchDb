namespace b0wter.CouchDb.Lib.Database

//
// Queries: /{db}/_all_docs
//

open b0wter.CouchDb.Lib
open b0wter.CouchDb.Lib.Core

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

    let private query (props: DbProperties.T) (dbName: string) (request: unit -> Async<FSharp.Data.HttpResponse>) =
        async {
            let request = createGet props (sprintf "%s/_all_docs" dbName) []
            let! result = sendRequest request
            let statusCode = result |> statusCodeFromResult
            let content = match result with | Ok o -> o.content | Error e -> e.reason
            match statusCode with
                | 200 -> 
                        match deserializeJson<Response> [] content with
                        | Ok r -> return Success r
                        | Error e -> return Failure <| errorRequestResult (statusCode, sprintf "Error: %s %s JSON: %s" e.reason System.Environment.NewLine e.json)
                | _ -> return Failure <| errorRequestResult (statusCode, content)
        }

    let queryAll (props: DbProperties.T) (dbName: string) : Async<Result> =
        let request = createGet props (sprintf "%s/_all_docs" dbName) []
        query props dbName request

    let querySelected (props: DbProperties.T) (dbName: string) (keys: string list) : Async<Result> =
        let keyCollection = { keys = keys }
        let request = createJsonPost props (sprintf "%s/_all_docs" dbName) keyCollection []
        query props dbName request



