namespace b0wter.CouchDb.Lib.Attachments

module PutBinary =

    open b0wter.CouchDb.Lib
    open b0wter.FSharp
    open b0wter.CouchDb.Lib.Core
    open b0wter.CouchDb.Lib.QueryParameters

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
        /// Specified database, document or attachment was not found (404)
        | NotFound of RequestResult.StringRequestResult
        /// Document’s revision wasn’t specified or it’s not the latest
        | Conflict of RequestResult.StringRequestResult
        /// Is returned before querying the db if the database name is empty.
        | DbNameMissing of RequestResult.StringRequestResult
        /// Local Json deserialization failed. A response from the server was received.
        | JsonDeserializationError of RequestResult.StringRequestResult
        /// If the result could not be interpreted.
        | Unknown of RequestResult.StringRequestResult
        /// An empty document id was given.
        | DocumentIdMissing of RequestResult.StringRequestResult
        /// An empty attachment name was given.
        | AttachmentNameMissing of RequestResult.StringRequestResult

    /// Adds a binary attachment to an existing document.
    let query<'a> dbProps dbName docId (docRev: string option) attachmentName attachment =
        async {
            if String.isNullOrWhiteSpace dbName then
                return Result.DbNameMissing <| RequestResult.createText (None, "The database name is empty. The query has not been sent to the server.")
            else if String.isNullOrWhiteSpace docId then
                return Result.DocumentIdMissing <| RequestResult.createText (None, "The document name is empty. The query has not been sent to the server.")
            else if String.isNullOrWhiteSpace attachmentName then
                return Result.AttachmentNameMissing <| RequestResult.createText (None, "The attachment name is empty. The query has not been sent to the server.")
            else
                let url = sprintf "%s/%s/%s" dbName docId attachmentName
                let queryParameters = match docRev with Some rev -> [ StringQueryParameter("rev", rev) :> BaseQueryParameter ] | None -> []
                let request = createBinaryPut dbProps url attachment queryParameters
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

    let asResult (r: Result) =
        match r with
        | Created x | Accepted x -> Ok x
        | BadRequest e | NotFound e | Unauthorized e | DbNameMissing e | DocumentIdMissing e | AttachmentNameMissing e | DocumentIdMissing e | JsonDeserializationError e | Conflict e | Unknown e ->
            Error <| ErrorRequestResult.fromRequestResultAndCase(e, r)   

    let queryAsResult dbProps dbName docId docRev attachmentName attachment = query dbProps dbName docId docRev attachmentName attachment |> Async.map asResult
