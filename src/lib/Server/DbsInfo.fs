namespace b0wter.CouchDb.Lib.Server

    //
    // Queries: /_dbs_info
    //
    
    open Newtonsoft.Json
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
            | KeyError of ErrorRequestResult
            /// <summary>
            /// Generic error case. Refer to the status code and reason for more details.
            /// </summary>
            | Failure of ErrorRequestResult

        /// <summary>
        /// Returns detailed information for all databases (`names` parameter).
        /// <summary>
        let query (props: DbProperties.T) (names: string list) : Async<Result> =
            async {
                do printfn "Querying db information for keys: %A" names
                if names.IsEmpty then
                    return Core.errorRequestResult (None, "The list of names is empty. The request was NOT send to the server", None)
                           |> Result.KeyError
                else
                    let payload = { keys = names}
                    let request = createJsonPost props "_dbs_info" payload []
                    let! result = sendRequest request |> Async.map (fun x -> x :> IRequestResult)
                    match result.StatusCode with
                    | Some 200 -> try
                                    return Success <| JsonConvert.DeserializeObject<Response []>(result.Body, Utilities.Json.jsonSettings)
                                  with
                                    | :? JsonException as ex  ->
                                        return Failure <| errorRequestResult (result.StatusCode, ex.Message, Some result.Headers)
                    | Some 400 -> return KeyError <| errorRequestResult (result.StatusCode, result.Body, Some result.Headers)
                    | _   -> return Failure <| errorRequestResult (result.StatusCode, result.Body, Some result.Headers)
            }
