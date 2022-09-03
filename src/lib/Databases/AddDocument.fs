namespace b0wter.CouchDb.Lib.Databases

//
// Queries: /{db} [POST]
//

open b0wter.CouchDb.Lib
open b0wter.CouchDb.Lib.Core
open Utilities

module AddDocument =
    type Response = {
        Id: string
        Ok: bool
        Rev: string
    }

    type Result
        /// Document created and stored on disk
        = Created of Response
        /// Document data accepted, but not yet stored on disk
        | Accepted of Response
        /// Write privileges required
        | Unauthorized of RequestResult.StringRequestResult
        /// Database doesn’t exist
        | DbDoesNotExist of RequestResult.StringRequestResult
        /// A Conflicting Document with same ID already exists
        | DocumentIdConflict of RequestResult.StringRequestResult
        /// A local json deserialization error occured.
        | JsonDeserializationError of RequestResult.StringRequestResult
        /// `obj` is null
        | DocumentIsNull of RequestResult.StringRequestResult
        /// Returned if the response could not be interpreted.
        | Unknown of RequestResult.StringRequestResult

    let query (props: DbProperties.DbProperties) (dbName: string) (obj: obj) =
        async {
            if obj |> isNull then return DocumentIsNull <| RequestResult.createText(None, "The document you supplied is null. No query has been sent to the server.") else
            let request = createJsonPost props dbName obj []
            let! result = sendTextRequest request 
            match result.StatusCode with
            | Some 201 | Some 202 ->
                return match deserializeJson<Response> result.Content with
                        | Ok o -> Created o
                        | Error e -> JsonDeserializationError <| RequestResult.createForJson(e, result.StatusCode, result.Headers)
            | Some 401 -> return Unauthorized result
            | Some 404 -> return DbDoesNotExist result
            | Some 409 -> return DocumentIdConflict result
            | x -> return Unknown <| RequestResult.createTextWithHeaders (x, result.Content, result.Headers)
        }

    /// Returns the result from the query as a generic `FSharp.Core.Result`.
    let asResult (r: Result) =
        match r with
        | Created x | Accepted x -> Ok x
        | Unauthorized e | DbDoesNotExist e | DocumentIdConflict e | JsonDeserializationError e | DocumentIsNull e | Unknown e ->
            Error <| ErrorRequestResult.fromRequestResultAndCase(e, r)
            
    /// Runs query followed by asResult.
    let queryAsResult props dbName obj = query props dbName obj |> Async.map asResult
