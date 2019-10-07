namespace b0wter.CouchDb.Lib.Documents

//
// Queries: /{db}/{docid} [HEAD]
//

open b0wter.CouchDb.Lib
open b0wter.CouchDb.Lib
open b0wter.CouchDb.Lib.Core
open b0wter.FSharp

module Head =
    
    type Response = {
        ETag: string
        Length: int
    }
    
    type Result
        /// <summary>
        /// Document exists
        /// </summary>
        = DocumentExists of Response
        /// <summary>
        /// Document wasn’t modified since specified revision
        /// </summary>
        | NotModified of Response
        /// <summary>
        /// Read privilege required
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
        /// Is returned if the response could not be interpreted as a case specified by the documentation.
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
    let query (props: DbProperties.T) (name: string) (id: obj) : Async<Result> =
        async {
            if System.String.IsNullOrWhiteSpace(name) then
                return DbNameMissing <| RequestResult.create (None, "The database name is empty. The query has not been sent to the server.")
            else if id = null then
                return DocumentIdMissing <| RequestResult.create (None, "The document id is empty. The query has not been sent to the server.")
            else
                let request = createHead props (sprintf "%s/%s" name (id |> string)) []
                let! result = sendRequest request
                let trimETag (tag: string) = tag.TrimStart([|'"'|]).TrimEnd([|'"'|])
                return match result.statusCode with
                       | Some 200 -> DocumentExists { ETag = result.headers.["ETag"] |> trimETag; Length = result.headers.["Content-Length"] |> int }
                       | Some 304 -> NotModified { ETag = result.headers.["ETag"] |> trimETag; Length = result.headers.["Content-Length"] |> int }
                       | Some 401 -> Unauthorized result
                       | Some 404 -> NotFound result
                       | _        -> Unknown result
        }
        
    /// Returns the result from the query as a generic `FSharp.Core.Result`.
    let asResult (r: Result) =
        match r with
        | DocumentExists x | NotModified x -> Ok x
        | NotFound e | Unauthorized e | DbNameMissing e | DocumentIdMissing e | Unknown e ->
            Error <| ErrorRequestResult.fromRequestResultAndCase(e, r)
            
    /// Runs query followed by asResult.
    let queryAsResult props name id = query props name id |> Async.map asResult
