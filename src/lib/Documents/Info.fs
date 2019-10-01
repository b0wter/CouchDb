namespace b0wter.CouchDb.Lib.Documents.Info

//
// Queries: /{db}/{docid} [HEAD]
//

open b0wter.CouchDb.Lib
open b0wter.CouchDb.Lib.Core
open System

module Exists =
    type Response = {
        ETag: string
        Length: int
    }
    
    type Result
        /// <summary>
        /// Is returned if the server gave a success status code.
        = Success of Response
        /// <summary>
        /// Is returned before a query is performed if either the db name or the id is empty/null.
        /// </summary>
        | RequestError of ErrorRequestResult
        /// <summary>
        /// Is returned of CouchDb returns a non-success status code or the request failed.
        /// </summary>
        | Failure of ErrorRequestResult
        (*
    let query (props: DbProperties.T) (name: string) (id: obj) : Async<Result> =
        async {
            if System.String.IsNullOrWhiteSpace(name) then
                return errorRequestResult(0, "DbName must not be null or empty.")
            else if id = null then
                return errorRequestResult(0, "Id must not be null")
            else
                let request = createHead props (sprintf "%s/%s" name (obj |> string))
                let! result = sendRequest request
                match result with
                | Ok o ->
                    o.
        }*)
