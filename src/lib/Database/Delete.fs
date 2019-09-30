namespace b0wter.CouchDb.Lib.Database

//
// Queries: /{db} [DELETE]
//

open b0wter.CouchDb.Lib
open b0wter.CouchDb.Lib.Core


module Delete =
    type Response = {
        ok: bool
    }

    let TrueCreateResult = { ok = true}
    let FalseCreateResult = { ok = false}
    
    /// <summary>
    /// Deleted - Database removed successfully (quorum is met and database is deleted by at least one node)
    /// Accepted - Accepted (deleted by at least one of the nodes, quorum is not met yet)
    /// Bad Request – Invalid database name or forgotten document id by accident
    /// Unauthorized – CouchDB Server Administrator privileges required
    /// Not Found – Database doesn’t exist or invalid database name
    /// Unknown - An error not spedified by the CouchDb documentation happened.
    /// </summary>
    type Result
        = Deleted of Response
        | Accepted of Response
        | NotFound of ErrorRequestResult
        | BadRequest of ErrorRequestResult
        | Unauthorized of ErrorRequestResult
        | Unknown of ErrorRequestResult

    let query (props: DbProperties.T) (name: string) : Async<Result> =
        async {
            let request = createDelete props name []
            let! result = sendRequest request 
            let statusCode = result |> statusCodeFromResult
            let content = match result with | Ok o -> o.content | Error e -> e.reason
            let r = match statusCode with
                    | 200 -> Deleted TrueCreateResult
                    | 202 -> Accepted TrueCreateResult
                    | 400 -> BadRequest <| errorRequestResult (statusCode, content)
                    | 401 -> Unauthorized <| errorRequestResult (statusCode, content)
                    | 404 -> NotFound <| errorRequestResult (statusCode, content)
                    | _   -> Unknown <| errorRequestResult (statusCode, content)
            return r
        }
        


