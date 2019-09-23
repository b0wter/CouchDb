namespace b0wter.CouchDb.Lib
open System.Runtime.Serialization

module Database =

    open Newtonsoft.Json
    open b0wter.FSharp
    open b0wter.CouchDb.Lib.Core

    module Exists =
        type Response = {
            alwaysEmpty: string
        }

        type Result
            = Exists
            | DoesNotExist
            | RequestError of ErrorRequestResult

        let query (props: DbProperties.T) (name: string) : Async<Result> =
            async {
                if System.String.IsNullOrWhiteSpace(name) then return RequestError <| errorRequestResult (0, "You need to set a database name.") else
                let request = createHead props name []
                match! sendRequest request with
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
            | Failure of ErrorRequestResult

        /// <summary>
        /// Returns a list of strings containing the names of all databases.
        /// </summary>
        let query (props: DbProperties.T) : Async<Result> =
            async {
                let request = createGet props "_all_dbs" []
                match! sendRequest request with
                | Ok o ->
                    try
                        return Success <| JsonConvert.DeserializeObject<string list>(o.content)
                    with
                    | :? JsonException as ex -> return Failure <| errorRequestResult (0, ex.Message)
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
    

    module Delete =
        type Response = {
            ok: bool
        }

        let TrueCreateResult = { ok = true}
        let FalseCreateResult = { ok = false}

        type Result
            = Deleted of Response
            | Accepted of Response
            | InvalidDatabase of ErrorRequestResult
            | Unauthorized of ErrorRequestResult
            | Unknown of ErrorRequestResult

        let query (props: DbProperties.T) (name: string) : Async<Result> =
            async {
                let request = createDelete props name []
                let! result = sendRequest request 
                let statusCode = result |> statusCodeFromResult
                let content = match result with | Ok o -> o.content | Error e -> e.reason
                let r = match statusCode with
                        | 200 -> Deleted TrueCreateResult
                        | 202 -> Accepted TrueCreateResult
                        | 400 -> InvalidDatabase <| errorRequestResult (statusCode, content)
                        | 401 -> Unauthorized <| errorRequestResult (statusCode, content)
                        | 404 -> InvalidDatabase <| errorRequestResult (statusCode, content)
                        | _   -> Unknown <| errorRequestResult (statusCode, content)
                return r
            }
            

    module AllDocuments =
        type ResponseRowValue = {
            rev: string
        }
        type ResponseRow = {
            id: System.Guid
            key: string
            value: ResponseRowValue option
        }
        type Response = {
            offset: int option
            rows: ResponseRow []
            total_rows: int
        }

        type Result
            = Success of Response
            | Failure of ErrorRequestResult

        type KeyCollection = {
            keys: string list
        }

        let private query (props: DbProperties.T) (dbName: string) (request: unit -> Async<FSharp.Data.HttpResponse>) =
            async {
                let request = createGet props (sprintf "%s/_all_docs" dbName) []
                let! result = sendRequest request
                let statusCode = result |> statusCodeFromResult
                let content = match result with | Ok o -> o.content | Error e -> e.reason
                match statusCode with
                    | 200 -> 
                            match deserializeJson<Response> [] content with
                            | Ok r -> return Success r
                            | Error e -> return Failure <| errorRequestResult (statusCode, sprintf "Error: %s %s JSON: %s" e.reason System.Environment.NewLine e.json)
                    | _ -> return Failure <| errorRequestResult (statusCode, content)
            }

        let queryAll (props: DbProperties.T) (dbName: string) : Async<Result> =
            let request = createGet props (sprintf "%s/_all_docs" dbName) []
            query props dbName request

        let querySelected (props: DbProperties.T) (dbName: string) (keys: string list) : Async<Result> =
            let keyCollection = { keys = keys }
            let request = createJsonPost props (sprintf "%s/_all_docs" dbName) keyCollection []
            query props dbName request

    module AddDocument =
        type Response = {
            id: string
            ok: bool
            rev: string
        }

        type Result
            = Created of Response
            | Accepted of Response
            | InvalidDatabaseName 
            | Unauthorized
            | DatabaseDoesNotExist
            | DocumentIdConflict
            | Failure of ErrorRequestResult

        let query (props: DbProperties.T) (dbName: string) (obj: obj) =
            async {
                let request = createJsonPost props dbName obj []
                let! result = sendRequest request
                let content = match result with | Ok o -> o.content | Error e -> e.reason
                match result |> statusCodeFromResult with
                | 201 | 202 ->
                    try
                        let response = JsonConvert.DeserializeObject<Response>(content)
                        return Created response
                    with
                    | :? JsonException as ex ->
                        return Failure <| errorRequestResult (0, ex.Message)
                | 400 -> return InvalidDatabaseName
                | 401 -> return Unauthorized
                | 404 -> return DatabaseDoesNotExist
                | 409 -> return DocumentIdConflict
                | x -> return Failure <| errorRequestResult (x, content)
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
            docs: 'a list
            warning: string option
            execution_stats: ExecutionStats option
            bookmarks: string option
        }

        type Result<'a>
            = Success of Response<'a>
            | InvalidRequest of ErrorRequestResult
            | NotAuthorized of ErrorRequestResult
            | QueryExecutionError of ErrorRequestResult
            | JsonError of JsonDeserialisationError
            | Failure of ErrorRequestResult

        let query<'a> (props: DbProperties.T) (dbName: string) (expression: Mango.Expression) =
            async {
                let request = createCustomJsonPost props (sprintf "%s/_find" dbName) [ MangoConverters.OperatorJsonConverter () :> JsonConverter ] expression []
                let! result = sendRequest request
                let queryResult = result |> statusCodeAndContent

                do printfn "Response content:%s%s" System.Environment.NewLine queryResult.content

                match queryResult.statusCode with
                | 200 ->
                    match deserializeJson<Response<'a>> [] queryResult.content with
                    | Ok o -> return Success o
                    | Error e -> return JsonError e
                | 400 | 401 | 500 ->
                    return InvalidRequest <| errorRequestResult (queryResult.statusCode, queryResult.content)
                | _ ->
                    return Failure <| errorRequestResult (queryResult.statusCode, queryResult.content)
            }
            // CouchDb contains a syntax to define the fields to return but since we are using Json-deserialization
            // this is currently not in use.
            