namespace b0wter.CouchDb.Lib.Databases

//
// Queries: /{db} [POST]
//

open b0wter.CouchDb.Lib
open b0wter.CouchDb.Lib.Core
open b0wter.FSharp

module AddDocument =
    type Response = {
        id: string
        ok: bool
        rev: string
    }

    type Result
        /// Document created and stored on disk
        = Created of Response
        /// Document data accepted, but not yet stored on disk
        | Accepted of Response
        /// Write privileges required
        | Unauthorized of RequestResult.T
        /// Database doesn’t exist
        | DbDoesNotExist of RequestResult.T
        /// A Conflicting Document with same ID already exists
        | DocumentIdConflict of RequestResult.T
        /// A local json deserialization error occured.
        | JsonDeserializationError of RequestResult.T
        /// `obj` is null
        | DocumentIsNull of RequestResult.T
        /// Returned if the response could not be interpreted.
        | Unknown of RequestResult.T

    let query (props: DbProperties.T) (dbName: string) (obj: obj) =
        async {
            if obj |> isNull then return DocumentIsNull <| RequestResult.create(None, "The document you supplied is null. No query has been sent to the server.") else
            let request = createJsonPost props dbName obj []
            let! result = sendRequest request 
            match result.statusCode with
            | Some 201 | Some 202 ->
                return match deserializeJson<Response> result.content with
                        | Ok o -> Created o
                        | Error e -> JsonDeserializationError <| RequestResult.createForJson(e, result.statusCode, result.headers)
            | Some 401 -> return Unauthorized result
            | Some 404 -> return DbDoesNotExist result
            | Some 409 -> return DocumentIdConflict result
            | x -> return Unknown <| RequestResult.createWithHeaders (x, result.content, result.headers)
        }

    /// Returns the result from the query as a generic `FSharp.Core.Result`.
    let asResult (r: Result) =
        match r with
        | Created x | Accepted x -> Ok x
        | Unauthorized e | DbDoesNotExist e | DocumentIdConflict e | JsonDeserializationError e | DocumentIsNull e | Unknown e ->
            Error <| ErrorRequestResult.fromRequestResultAndCase(e, r)
            
    /// Runs query followed by asResult.
    let queryAsResult props dbName obj = query props dbName obj |> Async.map asResult
