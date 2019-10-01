namespace b0wter.CouchDb.Lib.Documents.Info

//
// Queries: /{db}/{docid} [HEAD]
//

open b0wter.CouchDb.Lib
open b0wter.CouchDb.Lib.Core
open b0wter.FSharp

module MiniInfo =
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
        | NotAuthorized of ErrorRequestResult
        /// <summary>
        /// Document not found>
        /// </summary>
        | NotFound of ErrorRequestResult
        /// <summary>
        /// Is returned before querying the db if the database name is empty.
        /// </summary>
        | DbNameMissing
        /// <summary>
        /// Is returned before querying the db if the id is null.
        /// </summary>
        | DocumentIdMissing
        /// <summary>
        /// Is returned if the response could not be interpreted as a case specified by the documentation.
        /// </summary>
        | Failure of ErrorRequestResult
        
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
                return DbNameMissing
            else if id = null then
                return DocumentIdMissing
            else
                let request = createHead props (sprintf "%s/%s" name (obj |> string)) []
                let! result = sendRequest request |> Async.map (fun x -> x :> IRequestResult)
                return match result.StatusCode with
                       | Some 200 -> DocumentExists { ETag = result.Headers.["ETag"]; Length = result.Headers.["Content-Length"] |> int }
                       | Some 304 -> NotModified { ETag = result.Headers.["ETag"]; Length = result.Headers.["Content-Length"] |> int }
                       | Some 401 -> NotAuthorized <| errorFromIRequestResult result
                       | Some 404 -> NotFound <| errorFromIRequestResult result
                       | _        -> Failure <| errorFromIRequestResult result
        }
