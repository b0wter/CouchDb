namespace b0wter.CouchDb.Lib.Databases

//
// Queries: /{db}/_all_docs
//

open b0wter.CouchDb.Lib
open b0wter.CouchDb.Lib.Core
open b0wter.FSharp

module AllDocuments =
    type ResponseRowValue = {
        Rev: string
    }
    type ResponseRow = {
        Id: string
        Key: string
        Value: ResponseRowValue option
    }
    type Response = {
        Offset: int option
        Rows: ResponseRow list
        [<Newtonsoft.Json.JsonProperty("total_rows")>]
        TotalRows: int
    }

    type Result
        = Success of Response
        | BadRequest of RequestResult.TString
        | Unauthorized of RequestResult.TString
        | NotFound of RequestResult.TString
        | JsonDeserialisationError of RequestResult.TString
        | Unknown of RequestResult.TString

    type KeyCollection = {
        Keys: string list
    }

    let private query (request: Async<System.Net.Http.HttpResponseMessage>) =
        async {
            let! result = (sendTextRequest request)
            return match result.StatusCode with
                   | Some 200 -> 
                        match deserializeJsonWith<Response> [] result.Content with
                        | Ok r    -> Success r
                        | Error e -> JsonDeserialisationError <| RequestResult.createTextWithHeaders (result.StatusCode, sprintf "Error: %s %s JSON: %s" e.Reason System.Environment.NewLine e.Json, result.Headers)
                   | Some 400     -> BadRequest <| result
                   | Some 401     -> Unauthorized <| result
                   | Some 404     -> NotFound <| result
                   | _            -> Unknown <| result
        }

    let queryAll (props: DbProperties.T) (dbName: string) : Async<Result> =
        let request = createGet props (sprintf "%s/_all_docs" dbName) []
        query request

    let querySelected (props: DbProperties.T) (dbName: string) (keys: string list) : Async<Result> =
        let keyCollection = { Keys = keys }
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
