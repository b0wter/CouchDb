namespace b0wter.CouchDb.Lib

module Database =

    open Newtonsoft.Json

    module Exists =
        type Response = {
            alwaysEmpty: string
        }

        type Result
            = Exists
            | DoesNotExist
            | RequestError of Core.ErrorRequestResult

        let query (props: DbProperties.T) (name: string) : Async<Result> =
            async {
                let request = Core.createHead props name
                match! Core.sendRequest props request with
                | Ok o ->
                    let exists = o.statusCode = 200
                    return if exists then Exists else DoesNotExist
                | Error e when e.statusCode = 404 ->
                    return DoesNotExist
                | Error e ->
                    do printfn "Statuscode: %i" e.statusCode
                    return RequestError e
            }


    module All =
        type Response = string list

        type Result
            = Success of Response
            | Failure of Core.ErrorRequestResult

        /// <summary>
        /// Returns a list of strings containing the names of all databases.
        /// </summary>
        let query (props: DbProperties.T) : Async<Result> =
            async {
                let request = Core.createGet props "_all_dbs"
                match! Core.sendRequest props request with
                | Ok o ->
                    try
                        return Success <| JsonConvert.DeserializeObject<string list>(o.content)
                    with
                    | :? JsonException as ex -> return Failure <| Core.errorRequestResult (0, ex.Message)
                | Error e ->
                    return Failure e
            }


    module Create =
        type Response = {
            ok: bool
        }

        type Result
            = Created of Response
            | Accepted of Response
            | InvalidDbName of Response
            | Unauthorized of Response
            | AlreadyExists of Response
            | HttpError of Core.ErrorRequestResult
            | Unknown of Core.SuccessRequestResult

        let TrueCreateResult = { ok = true}
        let FalseCreateResult = { ok = false}

        let query (props: DbProperties.T) (name: string) : Async<Result> =
            async {
                let request = Core.createPut props name
                let! result = Core.sendRequest props request 
                let statusCode = result |> Core.statusCodeFromResult
                let content = match result with | Ok o -> o.content | Error e -> e.reason
                let r = match statusCode with
                        | 201 -> Created TrueCreateResult
                        | 202 -> Accepted TrueCreateResult
                        | 400 -> InvalidDbName FalseCreateResult
                        | 401 -> Unauthorized FalseCreateResult
                        | 412 -> AlreadyExists FalseCreateResult
                        | _   -> Unknown <| Core.successResultRequest (statusCode, content)
                return r
            }
    

    module Delete =
        type Response = {
            ok: bool
        }

        let TrueCreateResult = { ok = true}
        let FalseCreateResult = { ok = false}

        type Result
            = Deleted of Response
            | Accepted of Response
            | InvalidDatabase of Response
            | Unauthorized of Response
            | Unknown of Core.SuccessRequestResult

        let query (props: DbProperties.T) (name: string) : Async<Result> =
            async {
                let request = Core.createPut props name
                let! result = Core.sendRequest props request 
                let statusCode = result |> Core.statusCodeFromResult
                let content = match result with | Ok o -> o.content | Error e -> e.reason
                let r = match statusCode with
                        | 200 -> Deleted TrueCreateResult
                        | 202 -> Accepted TrueCreateResult
                        | 400 -> InvalidDatabase FalseCreateResult
                        | 401 -> Unauthorized FalseCreateResult
                        | 404 -> InvalidDatabase FalseCreateResult
                        | _   -> Unknown <| Core.successResultRequest (statusCode, content)
                return r
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

        type Response = {
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

        type MultipleNames = {
            keys: string list
        }

        type MultipleResult
            = Success of Response []
            | UnknownDatabase
            | Unknown of Core.SuccessRequestResult
            | Failure of Core.ErrorRequestResult

        type SingleResult
            = Success of Response
            | UnknownDatabase
            | Unknown of Core.SuccessRequestResult
            | Failure of Core.ErrorRequestResult

        let queryMultiple (props: DbProperties.T) (names: string list) : Async<MultipleResult> =
            async {
                do printfn "Querying db information for keys: %A" names
                let payload = { keys = names}
                let request = Core.createJsonPost props "_dbs_info" payload
                let! result = Core.sendRequest props request 
                let statusCode = result |> Core.statusCodeFromResult
                let content = match result with | Ok o -> o.content | Error e -> e.reason
                let r = match statusCode with
                        | 200 -> try
                                    MultipleResult.Success <| JsonConvert.DeserializeObject<Response []>(content)
                                 with
                                 | :? JsonException as ex  ->
                                    MultipleResult.Failure <| Core.errorRequestResult (0, ex.Message)
                        | 404 -> MultipleResult.UnknownDatabase
                        | _   -> MultipleResult.Unknown <| Core.successResultRequest (statusCode, content)
                return r
            }

        // TODO: merge with function above
        let querySingle (props: DbProperties.T) (name: string) : Async<SingleResult> =
            async {
                let request = Core.createGet props name
                let! result = Core.sendRequest props request 
                let statusCode = result |> Core.statusCodeFromResult
                let content = match result with | Ok o -> o.content | Error e -> e.reason
                let r = match statusCode with
                        | 200 -> try
                                    Success <| JsonConvert.DeserializeObject<Response>(content)
                                 with
                                 | :? JsonException as ex  ->
                                    Failure <| Core.errorRequestResult (0, ex.Message)
                        | 404 -> UnknownDatabase
                        | _   -> Unknown <| Core.successResultRequest (statusCode, content)
                return r
            }


    module AllDocuments =
        type Response = {
            offset: int
            rows: obj []
        }

        type Result
            = Success of Response
            | Failure of Core.ErrorRequestResult

        let query (props: DbProperties.T) : Async<Result> =
            async {
                return failwith "AllDocuments is not yet implemented!"
            }

    
    module Find =
        type ExecutionStats = {
            total_keys_examined: int
            total_docs_examined: int
            total_quorum_docs_examined: int
            results_returned: int
            execution_time_ms: float
        }

        type Response<'a> = {
            docs: 'a []
            warning: string option
            execution_stats: ExecutionStats
            bookmarks: string option
        }

        type Result<'a>
            = Success of Response<'a>
            | InvalidRequest of Core.ErrorRequestResult
            | NotAuthorized of Core.ErrorRequestResult
            | QueryExecutionError of Core.ErrorRequestResult

        let query (props: DbProperties.T)  =
            // CouchDb contains a syntax to define the fields to return but since we are using Json-deserialization
            // this is currently not in use.
            
            0