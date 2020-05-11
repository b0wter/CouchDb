namespace b0wter.CouchDb.Lib.Databases

//
// Queries: /{db}/_find [POST]
//

open b0wter.CouchDb.Lib
open b0wter.CouchDb.Lib.Core
open b0wter.FSharp
open Newtonsoft.Json
open b0wter.CouchDb.Lib

module Find =
    
    // TODO: Think about having two query methods: `query` and `queryWithExecutionStats`. This would remove the option.
    
    type ExecutionStats = {
        total_keys_examined: int
        total_docs_examined: int
        total_quorum_docs_examined: int
        results_returned: int
        execution_time_ms: float
    }

    type MetaData = {
        warning: string option
        execution_stats: ExecutionStats option
        bookmarks: string option
    }

    /// The documents of this response have already been parsed
    /// and converted into class/record instances.
    type Response<'a> = { 
        docs: 'a list
        warning: string option
        execution_stats: ExecutionStats option
        bookmarks: string option
    }


    type Result<'a>
        /// Request completed successfully
        = Success of Response<'a>
        /// Invalid request
        | BadRequest of RequestResult.T
        /// Read permission required
        | Unauthorized of RequestResult.T
        /// Query execution error
        | QueryExecutionError of RequestResult.T
        /// The database with the given name could not be found.
        | NotFound of RequestResult.T
        /// If the local deserialization of the servers response failed.
        | JsonDeserializationError of RequestResult.T
        /// If the response from the server could not be interpreted.
        | Unknown of RequestResult.T


    /// Turns a `RequestResult.T` into an actual `Result<'a>`.
    /// It will never return `Success` because that takes a `Response<'a>` as parameter.
    let private mapError (r: RequestResult.T) =
        match r.statusCode with
        | Some 400 -> BadRequest r
        | Some 401 -> Unauthorized r
        | Some 404 -> NotFound r
        | Some 500 -> QueryExecutionError r
        | _ ->
            Unknown r


    /// Returns the result from the query as a generic `FSharp.Core.Result`.
    let asResult<'a> (r: Result<'a>) =
        match r with
        | Success x -> Ok x
        | BadRequest e | Unauthorized e | QueryExecutionError e | JsonDeserializationError e | NotFound e | Unknown e ->
            Error <| ErrorRequestResult.fromRequestResultAndCase(e, r)


    /// Queries the server and does some basic parsing.
    /// The documents are not deserialized to objects but kept in a JObject list.
    /// This allows the user to perform dynamic operations.
    let private jObjectsQuery (printSerializedOperators: bool) (props: DbProperties.T) (dbName: string) (expression: Mango.Expression) =
        async {
            let request = createCustomJsonPost props (sprintf "%s/_find" dbName) [ (MangoConverters.OperatorJsonConverter(printSerializedOperators)) :> JsonConverter ] expression []
            let! result = sendRequest request
            //let queryResult = { QueryResult.content = result.content; QueryResult.statusCode = result.statusCode }
            if result.statusCode.IsSome && result.statusCode.Value = 200 then
                let objects = result.content |> Json.JObject.asJObject |> Result.bind (Json.JObject.getProperty "docs") |> Result.bind Json.JObject.getJArray |> Result.bind Json.JObject.jArrayAsJObjects
                let metadata = deserializeJson<MetaData> result.content

                return match objects, metadata with
                        | Ok a, Ok m -> Ok { Response.docs = a; Response.bookmarks = m.bookmarks; Response.execution_stats = m.execution_stats; Response.warning = m.warning }
                        | Error e, _ -> 
                            let jsonError = JsonDeserializationError.create(result.content, e)
                            let requestResult = RequestResult.createForJson(jsonError, result.statusCode, result.headers)
                            Error <| requestResult
                        | _, Error e ->
                            let requestResult = RequestResult.createForJson(e, result.statusCode, result.headers)
                            Error <| requestResult
            else
                return Error <| RequestResult.createWithHeaders(result.statusCode, result.content, result.headers)
        }


    /// Is build on top of `jObjectsQuery` and uses `Json.JObject.toObjects` to deserialize the `JObject list` into a list of actual objects.
    let private queryWith<'a> (printSerializedOperators: bool) (props: DbProperties.T) (dbName: string) (expression: Mango.Expression) : Async<Result<'a>> =
        async {
            match! jObjectsQuery printSerializedOperators props dbName expression with
            | Ok o -> 
                match o.docs |> Json.JObject.toObjects with
                | Ok docs -> 
                    return Success ({ Response.docs = docs; Response.bookmarks = o.bookmarks; Response.execution_stats = o.execution_stats; Response.warning = o.warning })
                | Error e -> 
                    let error = JsonDeserializationError.create(o.docs.ToString(), e)
                    return JsonDeserializationError (RequestResult.createForJson(error, None, Map.empty))
            | Error e -> return (mapError e)
        }


    /// Works like query but prints the serialized Find-Operators to stdout.
    let queryWithOutput<'a> (props: DbProperties.T) (dbName: string) (expression: Mango.Expression) =
        queryWith<'a> true props dbName expression


    /// Queries the database using a custom-built mango expression. 
    /// If you want to print the serialized operator use `queryWithOutput` instead.
    let query<'a> (props: DbProperties.T) (dbName: string) (expression: Mango.Expression) =
        queryWith<'a> false props dbName expression


    /// Is build on top of `jObjectsQuery` and maps the result into a `Database.Find.Result<JObject>`.
    let private queryJObjectsWith (printSerializedOperators: bool) (props: DbProperties.T) (dbName: string) (expression: Mango.Expression) : Async<Result<Linq.JObject>> =
        async {
            let! result = jObjectsQuery true props dbName expression
            return match result with
                    | Ok r -> Success r
                    | Error e -> mapError e
        }


    /// Queries the database using a custom-built mange expression.
    /// This version does not deserialize the documents but returns a `JObject list` instead.
    let queryJObjectsWithOutput = queryJObjectsWith true


    /// Runs `queryJObjectsWithOutput` followed by `asResult`.
    let queryJObjectsAsResultWithOutput props dbName expression = 
        queryJObjectsWithOutput props dbName expression |> Async.map asResult<Linq.JObject>


    /// Queries the database using a custom-built mange expression.
    /// This version does not deserialize the documents but returns a `JObject list` instead.
    let queryObjects = queryJObjectsWith false


    /// Runs `queryObjects` followed by `asResult`.
    let queryObjectsAsResult props dbName expression =
        queryObjects props dbName expression |> Async.map asResult<Linq.JObject>
        

    /// Runs `query` followed by `asResult`.
    let queryAsResult<'a> props dbName expression = query<'a> props dbName expression |> Async.map asResult<'a>
    

    /// Runs `queryWithOutput` followed by `asResult`.
    let queryAsResultWithOutput<'a> props dbName expression = queryWithOutput<'a> props dbName expression |> Async.map asResult<'a>


    /// Retrieves the first element of a successful query or an error message.
    /// Useful if you know that your query will return a single element.
    /// Also returns an error if the query is successful but did not return any documents.
    let getFirst (r: Result<'a>) : Result<'a, string> =
        r
        |> asResult
        |> Result.mapBoth (fun ok -> ok.docs |> List.tryHead)  (fun error -> sprintf "[%s] %s" error.case error.content)
        |> function
           | Ok (Some o) -> Ok o
           | Ok None -> Error "The query was successful but did not return any documents."
           | Error e -> Error e
