namespace b0wter.CouchDb.Lib.Documents

//
// Queries: /{db}/{docid} [COPY]
//

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
        | BadRequest of ErrorRequestResult
        /// Read or write privileges required (401)
        | Unauthorized of ErrorRequestResult
        /// Specified database or document ID or revision doesn’t exists (404)
        | NotFound of ErrorRequestResult
        /// Document with the specified ID already exists or specified revision is not latest for target document (409)
        | Conflict of ErrorRequestResult
        /// Is returned before querying the db if the database name is empty.
        | DbNameMissing
        /// Json deserialization failed
        | JsonDeserialisationError of ErrorRequestResult
        /// If the result could not be interpreted.
        | Unknown of ErrorRequestResult
        /// This endpoint requires the document id of the destination to be set.
        | DestinationIdMissing
    
    /// The COPY (which is non-standard HTTP) copies an existing document to a new or existing document.
    /// Copying a document is only possible within the same database.
    /// If the destination of the document already exists you need to supply the `destinationRev`.
    /// If you supply a `docRev` the given revision will be copied.
    let query<'a> (props: DbProperties.T) (dbName: string) (docId: System.Guid) (destinationId: System.Guid) (destinationRev: string option) (docRev: string option) : Async<Result> =
        async {
            if destinationId = System.Guid.Empty then
                return DestinationIdMissing
            else
                let destination = match destinationRev with
                                  | Some rev -> sprintf "%s?rev=%s" (destinationId |> string) rev
                                  | None -> destinationId |> string
                let destinationHeader = ("Destination", destination)
                let queryParams = match docRev with | Some rev -> [ StringQueryParameter("rev", rev)  :> BaseQueryParameter ] | None -> []
                let url = sprintf "%s/%s" dbName (docId |> string)
                let request = createCopy props url queryParams [ destinationHeader ]
                let! result = sendRequest request
                let iresult = result :> IRequestResult
                return match iresult.StatusCode with
                        | Some 201 ->
                            match deserializeJson [] iresult.Body with
                            | FSharp.Core.Result.Ok response -> Created response
                            | Error e -> JsonDeserialisationError <| errorRequestResult(iresult.StatusCode, sprintf "Reason: %s%sJson:%s" e.reason System.Environment.NewLine e.json, Some iresult.Headers)
                        | Some 202 ->
                            match deserializeJson [] iresult.Body with
                            | FSharp.Core.Result.Ok response -> Accepted response
                            | Error e -> JsonDeserialisationError <| errorRequestResult(iresult.StatusCode, sprintf "Reason: %s%sJson:%s" e.reason System.Environment.NewLine e.json, Some iresult.Headers)
                        | Some 400 -> BadRequest <| errorFromIRequestResult iresult
                        | Some 401 -> Unauthorized <| errorFromIRequestResult iresult
                        | Some 404 -> NotFound <| errorFromIRequestResult iresult
                        | Some 409 -> Conflict <| errorFromIRequestResult iresult
                        | _ -> Unknown <| errorFromIRequestResult iresult
        }
