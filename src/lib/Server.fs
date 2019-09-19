namespace b0wter.CouchDb.Lib

module Server =

    open Newtonsoft.Json
    open b0wter.CouchDb.Lib.Core

    module Info =
        type VendorInfo = {
            name: string
        }

        type Response = {
            couchdb: string
            uuid: System.Guid
            vendor: VendorInfo
            version: string
            git_sha: string
            features: string list
        }

        type Result
            = Success of Response
            | Failure of ErrorRequestResult

        let query (props: DbProperties.T) : Async<Result> =
            async {
                let request = createGet props "/" []
                match! sendRequest request with
                | Ok o ->
                    do printfn "%s" o.content
                    try
                        return Success <| JsonConvert.DeserializeObject<Response>(o.content)
                    with
                    | :? JsonException as ex -> return Failure <| errorRequestResult (0, ex.Message)
                | Error e ->
                    return Failure e
            }
        

    module Details =
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
                    return Core.errorRequestResult (400, "The list of names is empty. The request was NOT send to the server")
                           |> Result.KeyError
                else
                    let payload = { keys = names}

                    let request = createJsonPost props "_dbs_info" payload []
                    let! result = sendRequest request 
                    let statusCode = result |> statusCodeFromResult
                    let content = match result with | Ok o -> o.content | Error e -> e.reason
                    match statusCode with
                    | 200 -> try
                                return Success <| JsonConvert.DeserializeObject<Response []>(content, Utilities.Json.jsonSettings)
                             with
                             | :? JsonException as ex  ->
                                return Failure <| errorRequestResult (0, ex.Message)
                    | 400 -> return KeyError <| errorRequestResult (400, content)
                    | _   -> return Failure <| errorRequestResult (statusCode, content)
            }
