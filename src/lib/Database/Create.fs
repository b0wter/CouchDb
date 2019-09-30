namespace b0wter.CouchDb.Lib.Database

//
// Queries: /{db} [PUT]
//

open b0wter.CouchDb.Lib
open b0wter.CouchDb.Lib.Core

module Create =
    type Response = {
        ok: bool
    }

    type Result
        = Created of Response
        | Accepted of Response
        | InvalidDbName of ErrorRequestResult
        | Unauthorized of ErrorRequestResult
        | AlreadyExists of ErrorRequestResult
        | Unknown of SuccessRequestResult

    let TrueCreateResult = { ok = true}
    let FalseCreateResult = { ok = false}

    /// <summary>
    /// Runs a PUT query that will create a new database. The database name may only consist of the following characters:
    /// a-z, 0-9, _, $, (, ), +, -, /
    /// The name *must* begin with a lower-case letter.
    /// 
    /// `q`: Shards, aka the number of range partitions. Default is 8, unless overridden in the cluster config.
    /// 
    /// `n`: Replicas. The number of copies of the database in the cluster. The default is 3, unless overridden in the cluster config .
    /// </summary>
    let query (props: DbProperties.T) (name: string) (q: int option) (n: int option) : Async<Result> =
        async {
            if System.String.IsNullOrWhiteSpace(name) then return InvalidDbName <| errorRequestResult (0, "You need to set a database name.") else
            let parameters =
                [
                    (if q.IsSome then Some ("q", q.Value :> obj) else None)
                    (if n.IsSome then Some ("n", n.Value :> obj) else None)
                ] |> List.choose id
            let request = createPut props name parameters
            let! result = sendRequest request
            let statusCode = result |> statusCodeFromResult
            let content = match result with | Ok o -> o.content | Error e -> e.reason
            let r = match statusCode with
                    | 201 -> Created TrueCreateResult
                    | 202 -> Accepted TrueCreateResult
                    | 400 -> InvalidDbName <| errorRequestResult (statusCode, content)
                    | 401 -> Unauthorized <| errorRequestResult (statusCode, content)
                    | 412 -> AlreadyExists <| errorRequestResult (statusCode, content)
                    | _   -> Unknown <| successResultRequest (statusCode, content)
            return r
        }

