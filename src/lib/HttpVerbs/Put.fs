namespace b0wter.CouchDb.Lib.HttpVerbs

//
// Queries: /{db}/{docid} [PUT]
//

open b0wter.CouchDb.Lib
open b0wter.CouchDb.Lib.Core
open b0wter.CouchDb.Lib.QueryParameters
open b0wter.FSharp

module Put =

    type Response = {
        Id: string
        Ok: bool
        Rev: string
    }
    
    type Result
        /// Document created and stored on disk (201)
        = Created of Response
        /// Document data accepted, but not yet stored on disk (202)
        | Accepted of Response
        /// Invalid request body or parameters (400)
        | BadRequest of RequestResult.StringRequestResult
        /// Write privileges required (401)
        | Unauthorized of RequestResult.StringRequestResult
        /// Specified database or document ID doesn’t exists (404)
        | NotFound of RequestResult.StringRequestResult
        /// Document with the specified ID already exists or specified revision is not latest for target document (409)
        | Conflict of RequestResult.StringRequestResult
        /// Is returned before querying the db if the database name is empty.
        | DbNameMissing of RequestResult.StringRequestResult
        /// Json deserialization failed
        | JsonDeserializationError of RequestResult.StringRequestResult
        /// If the result could not be interpreted.
        | Unknown of RequestResult.StringRequestResult
        /// This endpoint requires the document id to be set.
        | DocumentIdMissing of RequestResult.StringRequestResult

    /// Unlike the POST /{db}, you must specify the document ID in the request URL.
    /// When updating an existing document, the current document revision must be included in the document 
    /// (i.e. the request body), as the rev query parameter, or in the If-Match request header.
    let query<'a> (props: DbProperties.DbProperties) (url: string) converters (docId: 'a -> string) (docRev: 'a -> string option) (document: 'a) : Async<Result> =
        async {
            if document |> docId |> String.isNullOrWhiteSpace then
                return DocumentIdMissing <| RequestResult.createText (None, "The document id is empty. The query has not been sent to the server.")
            else
                let queryParams = match document |> docRev with
                                  | Some rev -> [ StringQueryParameter("rev", rev) :> BaseQueryParameter ]
                                  | None -> []
                let request = createCustomJsonPut props url converters document queryParams
                let! result = sendTextRequest request
                return match result.StatusCode with
                        | Some 201 ->
                            match deserializeJsonWith [] result.Content with
                            | Ok response -> Created response
                            | Error e -> JsonDeserializationError <| RequestResult.createForJson(e, Some 201, result.Headers)
                        | Some 202 ->
                            match deserializeJsonWith [] result.Content with
                            | Ok response -> Accepted response
                            | Error e -> JsonDeserializationError <| RequestResult.createForJson(e, Some 202, result.Headers)
                        | Some 400 -> BadRequest result
                        | Some 401 -> Unauthorized result
                        | Some 404 -> NotFound result
                        | Some 409 -> Conflict result
                        | _ -> Unknown result
        }
        
    /// Returns the result from the query as a generic `FSharp.Core.Result`.
    let asResult (r: Result) =
        match r with
        | Created x | Accepted x -> Ok x
        | BadRequest e | NotFound e | Unauthorized e | DbNameMissing e | DocumentIdMissing e | JsonDeserializationError e | Conflict e | Unknown e ->
            Error <| ErrorRequestResult.fromRequestResultAndCase(e, r)
            
    /// Runs query followed by asResult.
    let queryAsResult<'a> props url converters docId docRev document = query<'a> props url converters docId docRev document |> Async.map asResult