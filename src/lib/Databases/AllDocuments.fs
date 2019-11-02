namespace b0wter.CouchDb.Lib.Databases

//
// Queries: /{db}/_all_docs
//

open b0wter.CouchDb.Lib
open b0wter.CouchDb.Lib
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
        rows: ResponseRow list
        total_rows: int
    }

    type Result
        = Success of Response
        | BadRequest of RequestResult.T
        | Unauthorized of RequestResult.T
        | NotFound of RequestResult.T
        | JsonDeserialisationError of RequestResult.T
        | Unknown of RequestResult.T

    type KeyCollection = {
        keys: string list
    }

    let private query (request: Async<System.Net.Http.HttpResponseMessage>) =
        async {
            let! result = (sendRequest request)
            return match result.statusCode with
                   | Some 200 -> 
                        match deserializeJsonWith<Response> [] result.content with
                        | Ok r    -> Success r
                        | Error e -> JsonDeserialisationError <| RequestResult.createWithHeaders (result.statusCode, sprintf "Error: %s %s JSON: %s" e.reason System.Environment.NewLine e.json, result.headers)
                   | Some 400     -> BadRequest <| result
                   | Some 401     -> Unauthorized <| result
                   | Some 404     -> NotFound <| result
                   | _            -> Unknown <| result
        }

    let queryAll (props: DbProperties.T) (dbName: string) : Async<Result> =
        let request = createGet props (sprintf "%s/_all_docs" dbName) []
        query request

    let querySelected (props: DbProperties.T) (dbName: string) (keys: string list) : Async<Result> =
        let keyCollection = { keys = keys }
        let request = createJsonPost props (sprintf "%s/_all_docs" dbName) keyCollection []
        query request

    /// Returns the result from the query as a generic `FSharp.Core.Result`.
    let asResult (r: Result) =
        match r with
        | Success x -> Ok x
        | NotFound e | JsonDeserialisationError e | Unauthorized e | BadRequest e | Unknown e -> Error <| ErrorRequestResult.fromRequestResultAndCase(e, r)

    /// Runs queryAll followed by asResult.
    let queryAllAsResult props dbName = queryAll props dbName |> Async.map asResult
    
    /// Runs querySelected followed by asResult.
    let querySelectedAsResult props dbName keys = querySelected props dbName keys |> Async.map asResult
