namespace b0wter.CouchDb.Lib.Database

//
// Queries: /{db} [PUT]
//

open b0wter.CouchDb.Lib
open b0wter.CouchDb.Lib.Core
open b0wter.FSharp

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
        | Unknown of ErrorRequestResult

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
            if System.String.IsNullOrWhiteSpace(name) then return InvalidDbName <| errorRequestResult (None, "You need to set a database name.", None) else
            let parameters =
                [
                    (if q.IsSome then Some ("q", q.Value :> obj) else None)
                    (if n.IsSome then Some ("n", n.Value :> obj) else None)
                ] |> List.choose id
            let request = createPut props name parameters
            let! result = (sendRequest request) |> Async.map (fun x -> x :> IRequestResult)
            let r = match result.StatusCode with
                    | Some 201 -> Created TrueCreateResult
                    | Some 202 -> Accepted TrueCreateResult
                    | Some 400 -> InvalidDbName <| errorRequestResult (result.StatusCode, result.Body, None)
                    | Some 401 -> Unauthorized <| errorRequestResult (result.StatusCode, result.Body, None)
                    | Some 412 -> AlreadyExists <| errorRequestResult (result.StatusCode, result.Body, None)
                    | _   -> Unknown <| errorRequestResult (result.StatusCode, result.Body, None)
            return r
        }

