namespace b0wter.CouchDb.Lib.Server

//
// Queries: /_dbs_info
//

open b0wter.CouchDb.Lib.Core
open b0wter.CouchDb.Lib
open b0wter.FSharp

module DbsInfo =
    type Cluster = {
        N: int
        Q: int
        R: int
        W: int
    }

    type Other = {
        [<Newtonsoft.Json.JsonProperty("data_size")>]
        DataSize: int
    }

    type Sizes = {
        Active: int
        External: int
        File: int
    }

    type Info = {
        Cluster: Cluster
        [<Newtonsoft.Json.JsonProperty("compact_running")>]
        CompactRunning: bool
        [<Newtonsoft.Json.JsonProperty("data_size")>]
        DataSize: int
        [<Newtonsoft.Json.JsonProperty("db_name")>]
        DbName: string
        [<Newtonsoft.Json.JsonProperty("disk_format_version")>]
        DiskFormatVersion: int
        [<Newtonsoft.Json.JsonProperty("disk_size")>]
        DiskSize: int
        [<Newtonsoft.Json.JsonProperty("doc_count")>]
        DocCount: int
        [<Newtonsoft.Json.JsonProperty("doc_del_count")>]
        DocDelCount: int
        [<Newtonsoft.Json.JsonProperty("instance_start_time")>]
        InstanceStartTime: string
        [<Newtonsoft.Json.JsonProperty("purge_seq")>]
        PurgeSeq: string
        Sizes: Sizes
        [<Newtonsoft.Json.JsonProperty("update_seq")>]
        UpdateSeq: string
    }

    type Response = {
        Key: string 
        Info: Info option
    }

    type MultipleNames = {
        Keys: string list
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
        | KeyError of RequestResult.TString
        /// Returned if the local deserialization of the response failed.
        | JsonDeserialisationError of RequestResult.TString
        /// <summary>
        /// Generic error case. Refer to the status code and reason for more details.
        /// </summary>
        | Unknown of RequestResult.TString

    /// <summary>
    /// Returns detailed information for all databases (`names` parameter).
    /// </summary>
    let query (props: DbProperties.T) (names: string list) : Async<Result> =
        async {
            if names.IsEmpty then
                return KeyError <| RequestResult.createText (None, "You have not supplied database names. No query was sent to the server.")
            else
                let payload = { Keys = names}
                let request = createJsonPost props "_dbs_info" payload []
                let! result = sendTextRequest request 
                return match result.StatusCode with
                        | Some 200 -> match deserializeJson result.Content with
                                      | Ok r -> Success r
                                      | Error e -> JsonDeserialisationError <| RequestResult.createForJson(e, result.StatusCode, result.Headers)
                        | Some 400 -> KeyError <| RequestResult.createTextWithHeaders (result.StatusCode, result.Content, result.Headers)
                        | _   -> Unknown <| RequestResult.createTextWithHeaders (result.StatusCode, result.Content, result.Headers)
        }
        
    /// Returns the result from the query as a generic `FSharp.Core.Result`.
    let asResult (r: Result) =
        match r with
        | Success response -> Ok response
        | KeyError e | JsonDeserialisationError e | Unknown e -> Error <| ErrorRequestResult.fromRequestResultAndCase(e, r)
        
    /// Runs query followed by asResult.
    let queryAsResult props names = query props names |> Async.map asResult
