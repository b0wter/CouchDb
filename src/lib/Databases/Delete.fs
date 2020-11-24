namespace b0wter.CouchDb.Lib.Databases

//
// Queries: /{db} [DELETE]
//

open b0wter.CouchDb.Lib
open b0wter.CouchDb.Lib.Core
open b0wter.FSharp


module Delete =
    type Response = {
        Ok: bool
    }

    let TrueCreateResult = { Ok = true}
    let FalseCreateResult = { Ok = false}
    
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
        | NotFound of RequestResult.TString
        | BadRequest of RequestResult.TString
        | Unauthorized of RequestResult.TString
        | Unknown of RequestResult.TString

    let query (props: DbProperties.T) (name: string) : Async<Result> =
        async {
            let request = createDelete props name []
            let! result = sendTextRequest request
            let r = match result.StatusCode with
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

    /// Runs query followed by asResult.
    let queryAsResult props name = query props name |> Async.map asResult
