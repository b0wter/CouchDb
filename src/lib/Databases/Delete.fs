namespace b0wter.CouchDb.Lib.Database

//
// Queries: /{db} [DELETE]
//

open b0wter.CouchDb.Lib
open b0wter.CouchDb.Lib.Core
open b0wter.FSharp


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
        | NotFound of RequestResult.T
        | BadRequest of RequestResult.T
        | Unauthorized of RequestResult.T
        | Unknown of RequestResult.T

    let query (props: DbProperties.T) (name: string) : Async<Result> =
        async {
            let request = createDelete props name []
            let! result = sendRequest request
            let r = match result.statusCode with
                    | Some 200 -> Deleted TrueCreateResult
                    | Some 202 -> Accepted TrueCreateResult
                    | Some 400 -> BadRequest result
                    | Some 401 -> Unauthorized result
                    | Some 404 -> NotFound result
                    | _   -> Unknown result
            return r
        }
        
    /// Returns the result from the query as a generic `FSharp.Core.Result`.
    let asResult (r: Result) =
        match r with
        | Deleted x | Accepted x -> Ok x
        | NotFound e | Unauthorized e | BadRequest e | Unknown e -> Error <| ErrorRequestResult.fromRequestResultAndCase(e, r)


