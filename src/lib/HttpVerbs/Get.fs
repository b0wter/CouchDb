namespace b0wter.CouchDb.Lib.HttpVerbs

//
// Queries: /{db}/{docid} [GET]
//

open Newtonsoft.Json
open b0wter.CouchDb.Lib
open b0wter.CouchDb.Lib.Core
open b0wter.CouchDb.Lib.QueryParameters
open b0wter.FSharp

module Get =
    
    // TODO: Check if query parameters actually work!
    
    module QueryParameters =
        
        let IncludeAttachments = TrueQueryParameter("attachments") 
        let AttachmentEncodingInfo = TrueQueryParameter("attachments")
        let AttachmentsSince revs = StringListQueryParameter("atts_since", revs)
        let ConflictsInfo = TrueQueryParameter("conflicts")
        let DetectedConflictRevisions = TrueQueryParameter("detected_conflicts")
        let ForceRetrievalOfLatest = TrueQueryParameter("latest")
        let IncludeLastUpdateSequence = TrueQueryParameter("local_seq")
        let IncludeAllMetaInfo = TrueQueryParameter("meta")
        let OpenRevs revs = StringListQueryParameter("open_revs", revs)
        let SelectRevision rev = StringQueryParameter("rev", rev)
        let IncludeRevisionInfo = TrueQueryParameter("revs_info")
        
    type MetaFields = {
        /// Deletion flag. Available if document was removed
        _deleted: bool
        /// Attachment’s stubs. Available if document has any attachments
        _attachments: obj list
        /// List of conflicted revisions. Available if requested with `conflicts=true` query parameter
        _conflicts: string list
        /// List of deleted conflicted revisions. Available if requested with `deleted_conflicts=true` query parameter
        _deleted_conflicts: string list
        /// Document’s update sequence in current database. Available if requested with `local_seq=true` query parameter
        _local_seq: string
        /// List of objects with information about local revisions and their status.
        /// Available if requested with `open_revs` query parameter
        _revs_info: string list
        /// List of local revision tokens without. Available if requested with `revs=true` query parameter
        _revisions: string list
    }
    
    type Response<'a> = {
        /// Meta information for this document
        meta: MetaFields
        /// Actual document.
        content: 'a
    }
    
    type Result<'a>
        /// <summary>
        /// Document exists
        /// </summary>
        = DocumentExists of Response<'a>
        /// <summary>
        /// Document wasn’t modified since specified revision
        /// </summary>
        | NotModified of Response<'a>
        /// <summary>
        /// Read privilege required
        /// </summary>
        | Unauthorized of RequestResult.T
        /// <summary>
        /// Document not found>
        /// </summary>
        | NotFound of RequestResult.T
        /// <summary>
        /// Is returned before querying the db if the database name is empty.
        /// </summary>
        | DbNameMissing of RequestResult.T
        /// <summary>
        /// Is returned before querying the db if the id is null.
        /// </summary>
        | DocumentIdMissing of RequestResult.T
        /// <summary>
        /// Is returned if the query was successful but the local deserialization failed.
        /// </summary>
        | JsonDeserializationError of RequestResult.T
        /// <summary>
        /// Is returned if the response could not be interpreted as a case specified by the documentation
        /// or a network level error ocurred.
        /// </summary>
        | Unknown of RequestResult.T
        
    /// <summary>
    /// Returns the HTTP Headers containing a minimal amount of information about the specified document.
    /// The method supports the same query arguments as the GET /{db}/{docid} method, but only the header
    /// information (including document size, and the revision as an ETag), is returned.
    ///
    /// The ETag header shows the current revision for the requested document, and the Content-Length specifies
    /// the length of the data, if the document were requested in full.
    ///
    /// The given `id` will be converted to a string using the ToString() method.
    /// </summary>
    let query<'a> (props: DbProperties.T) (name: string) (url: string) (id: System.Guid) (queryParameters: BaseQueryParameter list): Async<Result<'a>> =
        async {
            if id = System.Guid.Empty then
                return DocumentIdMissing <| RequestResult.create (None, "The document id is empty. The query has not been sent to the server.")
            else
                let request = createGet props url queryParameters
                let! result = sendRequest request
                return match result.statusCode with
                       | Some 200 | Some 304 ->
                         let document = result.content |> deserializeJson<'a>
                         let meta = result.content |> deserializeJson<MetaFields>
                         match (document, meta) with
                         | (Ok d, Ok m) -> DocumentExists { meta = m; content = d }
                         | (Error e, _) -> JsonDeserializationError <| RequestResult.createForJson(e, result.statusCode, result.headers)
                         | (_, Error e) -> JsonDeserializationError <| RequestResult.createForJson(e, result.statusCode, result.headers)
                       | Some 401 -> Unauthorized result
                       | Some 404 -> NotFound result
                       | _        -> Unknown result
        }

    /// Returns the result from the query as a generic `FSharp.Core.Result`.
    let asResult<'a> (r: Result<'a>) =
        match r with
        | DocumentExists x | NotModified x -> Ok x
        | NotFound e | Unauthorized e | DbNameMissing e | DocumentIdMissing e | JsonDeserializationError e | Unknown e ->
            Error <| ErrorRequestResult.fromRequestResultAndCase(e, r)
            
    /// Runs query followed by asResult.
    let queryAsResult<'a> props name url id queryParameters = query<'a> props name url id queryParameters |> Async.map asResult<'a>
