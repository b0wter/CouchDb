namespace b0wter.CouchDb.Lib.Documents

//
// Queries: /{db}/{docid} [COPY]
//

open b0wter.CouchDb.Lib
open b0wter.CouchDb.Lib
open b0wter.CouchDb.Lib.Core
open b0wter.CouchDb.Lib.QueryParameters

module Copy =
    
    type Response = {
        id: System.Guid
        ok: bool
        rev: string
    }

    type Result
        /// Document created and stored on disk (201)
        = Created of Response
        /// Document data accepted, but not yet stored on disk (202)
        | Accepted of Response
        /// Invalid request body or parameters (400)
        | BadRequest of RequestResult.T
        /// Read or write privileges required (401)
        | Unauthorized of RequestResult.T
        /// Specified database or document ID or revision doesn’t exists (404)
        | NotFound of RequestResult.T
        /// Document with the specified ID already exists or specified revision is not latest for target document (409)
        | Conflict of RequestResult.T
        /// Is returned before querying the db if the database name is empty.
        | DbNameMissing of RequestResult.T
        /// Json deserialization failed
        | JsonDeserializationError of RequestResult.T
        /// If the result could not be interpreted.
        | Unknown of RequestResult.T
        /// This endpoint requires the document id of the destination to be set.
        | DestinationIdMissing of RequestResult.T
        /// This endpoint requires the document id to be set.
        | DocumentIdMissing of RequestResult.T
    
    /// The COPY (which is non-standard HTTP) copies an existing document to a new or existing document.
    /// Copying a document is only possible within the same database.
    /// If the destination of the document already exists you need to supply the `destinationRev`.
    /// If you supply a `docRev` the given revision will be copied.
    let query<'a> (props: DbProperties.T) (dbName: string) (docId: System.Guid) (destinationId: System.Guid) (destinationRev: string option) (docRev: string option) : Async<Result> =
        async {
            if destinationId = System.Guid.Empty then
                return DestinationIdMissing <| RequestResult.create(None, "You need to supply a non-empty destination id. The query has not been sent to the server.")
            else if docId = System.Guid.Empty then
                return DocumentIdMissing <| RequestResult.create (None, "The document id is empty. The query has not been sent to the server.")
            else
                let destination = match destinationRev with
                                  | Some rev -> sprintf "%s?rev=%s" (destinationId |> string) rev
                                  | None -> destinationId |> string
                let destinationHeader = ("Destination", destination)
                let queryParams = match docRev with | Some rev -> [ StringQueryParameter("rev", rev)  :> BaseQueryParameter ] | None -> []
                let url = sprintf "%s/%s" dbName (docId |> string)
                let request = createCopy props url queryParams [ destinationHeader ]
                let! result = sendRequest request
                return match result.statusCode with
                        | Some 201 ->
                            match deserializeJsonWith [] result.content with
                            | FSharp.Core.Result.Ok response -> Created response
                            | Error e -> JsonDeserializationError <| RequestResult.createWithHeaders (result.statusCode, sprintf "Reason: %s%sJson:%s" e.reason System.Environment.NewLine e.json, result.headers)
                        | Some 202 ->
                            match deserializeJsonWith [] result.content with
                            | FSharp.Core.Result.Ok response -> Accepted response
                            | Error e -> JsonDeserializationError <| RequestResult.createWithHeaders (result.statusCode, sprintf "Reason: %s%sJson:%s" e.reason System.Environment.NewLine e.json, result.headers)
                        | Some 400 -> BadRequest <| result
                        | Some 401 -> Unauthorized <| result
                        | Some 404 -> NotFound <| result
                        | Some 409 -> Conflict <| result
                        | _ -> Unknown <| result
        }

    /// Returns the result from the query as a generic `FSharp.Core.Result`.
    let asResult (r: Result) =
        match r with
        | Created x | Accepted x -> Ok x
        | BadRequest e | NotFound e | Unauthorized e | DbNameMissing e | DocumentIdMissing e | JsonDeserializationError e | Conflict e | Unknown e | DestinationIdMissing e ->
            Error <| ErrorRequestResult.fromRequestResultAndCase(e, r)