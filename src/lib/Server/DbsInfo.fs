namespace b0wter.CouchDb.Lib.Server

//
// Queries: /_dbs_info
//

open b0wter.CouchDb.Lib.Core
open b0wter.CouchDb.Lib
open b0wter.FSharp

module DbsInfo =
    type Cluster = {
        n: int
        q: int
        r: int
        w: int
    }

    type Other = {
        data_size: int
    }

    type Sizes = {
        active: int
        external: int
        file: int
    }

    type Info = {
        cluster: Cluster
        compact_running: bool
        data_size: int
        db_name: string
        disk_format_version: int
        disk_size: int
        doc_count: int
        doc_del_count: int
        instance_start_time: string
        purge_seq: string
        sizes: Sizes
        update_seq: string
    }

    type Response = {
        key: string 
        info: Info option
    }

    type MultipleNames = {
        keys: string list
    }

    type Result
        /// <summary>
        /// Returned if the request was answered successfully. Does not mean that infos for each db are valid!
        /// If you specify an unknown database CouchDB will return "null" (converted to None).
        /// </summary>
        = Success of Response []
        /// <summary>
        /// Returned if the keys-field is missing or the number of requested keys exceeded the server's maximum allowed number of keys.
        /// </summary>
        | KeyError of RequestResult.T
        /// Returned if the local deserialization of the response failed.
        | JsonDeserialisationError of RequestResult.T
        /// <summary>
        /// Generic error case. Refer to the status code and reason for more details.
        /// </summary>
        | Unknown of RequestResult.T

    /// <summary>
    /// Returns detailed information for all databases (`names` parameter).
    /// </summary>
    let query (props: DbProperties.T) (names: string list) : Async<Result> =
        async {
            do printfn "Querying db information for keys: %A" names
            if names.IsEmpty then
                return KeyError <| RequestResult.create (None, "You have not supplied database names. No query was sent to the server.")
            else
                let payload = { keys = names}
                let request = createJsonPost props "_dbs_info" payload []
                let! result = sendRequest request 
                return match result.statusCode with
                        | Some 200 -> match deserializeJson result.content with
                                      | Ok r -> Success r
                                      | Error e -> JsonDeserialisationError <| RequestResult.createForJson(e, result.statusCode, result.headers)
                        | Some 400 -> KeyError <| RequestResult.createWithHeaders (result.statusCode, result.content, result.headers)
                        | _   -> Unknown <| RequestResult.createWithHeaders (result.statusCode, result.content, result.headers)
        }
        
    /// Returns the result from the query as a generic `FSharp.Core.Result`.
    let asResult (r: Result) =
        match r with
        | Success response -> Ok response
        | KeyError e | JsonDeserialisationError e | Unknown e -> Error <| ErrorRequestResult.fromRequestResultAndCase(e, r)
        
    /// Runs query followed by asResult.
    let queryAsResult props names = query props names |> Async.map asResult
