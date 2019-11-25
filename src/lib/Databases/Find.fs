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

    let private queryWith<'a> (printSerializedOperators: bool) (props: DbProperties.T) (dbName: string) (expression: Mango.Expression) =
        async {
            let request = createCustomJsonPost props (sprintf "%s/_find" dbName) [ (MangoConverters.OperatorJsonConverter(printSerializedOperators)) :> JsonConverter ] expression []
            let! result = sendRequest request
            let queryResult = { QueryResult.content = result.content; QueryResult.statusCode = result.statusCode }

            return match queryResult.statusCode with
                    | Some 200 ->
                        match deserializeJsonWith<Response<'a>> [] queryResult.content with
                        | Ok o -> Success o
                        | Error e -> JsonDeserializationError <| RequestResult.createForJson(e, result.statusCode, result.headers)
                    | Some 400 -> BadRequest result
                    | Some 401 -> Unauthorized result
                    | Some 404 -> NotFound result
                    | Some 500 -> QueryExecutionError result
                    | _ ->
                        Unknown result
        }
    
    /// Works like query but prints the serialized Find-Operators to stdout.
    let queryWithOutput<'a> (props: DbProperties.T) (dbName: string) (expression: Mango.Expression) =
        queryWith true props dbName expression

    /// Queries the database using a custom-build mango expression. 
    /// If you want to print the serialized operator use `queryWithOutput` instead.
    let query<'a> (props: DbProperties.T) (dbName: string) (expression: Mango.Expression) =
        queryWith false props dbName expression
        
    /// Returns the result from the query as a generic `FSharp.Core.Result`.
    let asResult<'a> (r: Result<'a>) =
        match r with
        | Success x -> Ok x
        | BadRequest e | Unauthorized e | QueryExecutionError e | JsonDeserializationError e | NotFound e | Unknown e ->
            Error <| ErrorRequestResult.fromRequestResultAndCase(e, r)
            
    /// Runs query followed by asResult.
    let queryAsResult<'a> props dbName expression = query<'a> props dbName expression |> Async.map asResult<'a>
    
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
