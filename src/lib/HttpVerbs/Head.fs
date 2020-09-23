namespace b0wter.CouchDb.Lib.HttpVerbs

//
// Queries: /{db}/{docid} [HEAD]
//

open b0wter.CouchDb.Lib
open b0wter.CouchDb.Lib
open b0wter.CouchDb.Lib.Core
open b0wter.FSharp

module Head =
    
    /// Should also contain the content-lenght but System.Net.Http.HttpClient hides
    /// the header until after it finished processing the stream!
    /// See: https://social.msdn.microsoft.com/Forums/windowsapps/en-US/cb7417b5-ca3e-44f6-a272-9e2f8fc5d9b8 \
    ///      /portable-httpclient-hides-contentlength-and-contentencoding-headers-with-gzip-encoding?forum=wpdevelop
    type Response = {
        ETag: string
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
    let query(props: DbProperties.T) (url: string) (id: string) : Async<Result> =
        async {
            if String.isNullOrWhiteSpace id then
                return DocumentIdMissing <| RequestResult.create (None, "The document id is empty. The query has not been sent to the server.")
            else
                let request = createHead props url []
                let! result = sendRequest request
                let trimETag (tag: string) = tag.TrimStart([|'"'|]).TrimEnd([|'"'|])

                return match result.statusCode with
                       | Some 200 -> 
                            if result.headers.ContainsKey("ETag") then
                                DocumentExists { ETag = result.headers.["ETag"] |> trimETag }
                            else
                                Unknown result
                       | Some 304 -> 
                            if result.headers.ContainsKey("ETag") && result.headers.ContainsKey("Content-Length") then
                                NotModified { ETag = result.headers.["ETag"] |> trimETag }
                            else
                                Unknown result
                       | Some 401 -> Unauthorized result
                       | Some 404 -> NotFound result
                       | _        -> Unknown result
        }
        
    /// Returns the result from the query as a generic `FSharp.Core.Result`.
    /// `DocumentExists` and `NotModified` are mapped to `true`, the other
    /// results are mapped to `false`.
    let asResult (r: Result) =
        match r with
        | DocumentExists x | NotModified x -> Ok x
        | NotFound e | Unauthorized e | DbNameMissing e | DocumentIdMissing e | Unknown e ->
            Error <| ErrorRequestResult.fromRequestResultAndCase(e, r)
            
    /// Runs query followed by asResult.
    let queryAsResult props url id = query props url id |> Async.map asResult
