namespace b0wter.CouchDb.Lib

module Database =

    open Newtonsoft.Json
    open b0wter.FSharp

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
            | Failure of Core.ErrorRequestResult

        type KeyCollection = {
            keys: string list
        }

        let private query (props: DbProperties.T) (dbName: string) (request: unit -> Async<FSharp.Data.HttpResponse>) =
            async {
                
                let! result = Core.sendRequest props request
                let statusCode = result |> Core.statusCodeFromResult
                let content = match result with | Ok o -> o.content | Error e -> e.reason
                match statusCode with
                    | 200 -> 
                            match Core.deserializeJson<Response> [] content with
                            | Ok r -> return Success r
                            | Error e -> return Failure <| Core.errorRequestResult (statusCode, sprintf "Error: %s %s JSON: %s" e.reason System.Environment.NewLine e.json)
                    | _ -> return Failure <| Core.errorRequestResult (statusCode, content)
            }

        let queryAll (props: DbProperties.T) (dbName: string) : Async<Result> =
            let request = Core.createGet props (sprintf "%s/_all_docs" dbName)
            query props dbName request

        let querySelected (props: DbProperties.T) (dbName: string) (keys: string list) : Async<Result> =
            let keyCollection = { keys = keys }
            let request = Core.createJsonPost props (sprintf "%s/_all_docs" dbName) keyCollection
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
            | Failure of Core.ErrorRequestResult

        let query (props: DbProperties.T) (dbName: string) (obj: obj) =
            async {
                let request = Core.createJsonPost props dbName obj
                let! result = Core.sendRequest props request
                let content = match result with | Ok o -> o.content | Error e -> e.reason
                match result |> Core.statusCodeFromResult with
                | 201 | 202 ->
                    try
                        let response = JsonConvert.DeserializeObject<Response>(content)
                        return Created response
                    with
                    | :? JsonException as ex ->
                        return Failure <| Core.errorRequestResult (0, ex.Message)
                | 400 -> return InvalidDatabaseName
                | 401 -> return Unauthorized
                | 404 -> return DatabaseDoesNotExist
                | 409 -> return DocumentIdConflict
                | x -> return Failure <| Core.errorRequestResult (x, content)
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
            | InvalidRequest of Core.ErrorRequestResult
            | NotAuthorized of Core.ErrorRequestResult
            | QueryExecutionError of Core.ErrorRequestResult
            | JsonError of Core.JsonDeserialisationError
            | Failure of Core.ErrorRequestResult

        let query<'a> (props: DbProperties.T) (dbName: string) (expression: Mango.Expression) =
            async {
                let request = Core.createCustomJsonPost props (sprintf "%s/_find" dbName) [ MangoConverters.ExpressionJsonConverter () :> JsonConverter ] expression
                let! result = Core.sendRequest props request
                let queryResult = result |> Core.statusCodeAndContent

                match queryResult.statusCode with
                | 200 ->
                    match Core.deserializeJson<Response<'a>> [] queryResult.content with
                    | Ok o -> return Success o
                    | Error e -> return JsonError e
                | 400 | 401 | 500 ->
                    return InvalidRequest <| Core.errorRequestResult (queryResult.statusCode, queryResult.content)
                | _ ->
                    return Failure <| Core.errorRequestResult (queryResult.statusCode, queryResult.content)
            }
            // CouchDb contains a syntax to define the fields to return but since we are using Json-deserialization
            // this is currently not in use.
            