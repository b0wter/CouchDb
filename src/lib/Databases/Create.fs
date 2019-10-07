namespace b0wter.CouchDb.Lib.Databases

//
// Queries: /{db} [PUT]
//

open b0wter.CouchDb.Lib
open b0wter.CouchDb.Lib.Core
open b0wter.FSharp
open QueryParameters
open b0wter.CouchDb.Lib

module Create =
    
    module QueryParameters =
        /// `q`: Shards, aka the number of range partitions. Default is 8, unless overridden in the cluster config.
        let ShardRangePartition q = IntQueryParameter("q", q)
        /// `n`: Replicas. The number of copies of the database in the cluster. The default is 3, unless overridden in the cluster config .
        let Replicas r = IntQueryParameter("r", r)
    
    type Response = {
        ok: bool
    }

    type Result
        = Created of Response
        | Accepted of Response
        | InvalidDbName of RequestResult.T
        | Unauthorized of RequestResult.T
        | AlreadyExists of RequestResult.T
        | Unknown of RequestResult.T

    let TrueCreateResult = { ok = true}
    let FalseCreateResult = { ok = false}

    /// <summary>
    /// Runs a PUT query that will create a new database. The database name may only consist of the following characters:
    /// a-z, 0-9, _, $, (, ), +, -, /
    /// The name *must* begin with a lower-case letter.
    /// </summary>
    let query (props: DbProperties.T) (name: string) (parameters: QueryParameters) : Async<Result> =
        async {
            if System.String.IsNullOrWhiteSpace(name) then return InvalidDbName <| RequestResult.create (None, "You need to set a database name.") else
            let request = createPut props name parameters
            let! result = (sendRequest request) 
            let r = match result.statusCode with
                    | Some 201 -> Created TrueCreateResult
                    | Some 202 -> Accepted TrueCreateResult
                    | Some 400 -> InvalidDbName <| result
                    | Some 401 -> Unauthorized <| result
                    | Some 412 -> AlreadyExists <| result
                    | _   -> Unknown <| result
            return r
        }
        
    /// Returns the result from the query as a generic `FSharp.Core.Result`.
    let asResult (r: Result) =
        match r with
        | Created x | Accepted x -> Ok x
        | InvalidDbName e | Unauthorized e | AlreadyExists e | Unknown e -> Error <| ErrorRequestResult.fromRequestResultAndCase(e, r)
    
    /// Runs query followed by asResult.
    let queryAsResult props name parameters = query props name parameters |> Async.map asResult
