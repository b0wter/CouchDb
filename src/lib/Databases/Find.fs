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
        /// If the local deserialization of the servers response failed.
        | JsonDeserializationError of RequestResult.T
        /// If the response from the server could not be interpreted.
        | Unknown of RequestResult.T

    let query<'a> (props: DbProperties.T) (dbName: string) (expression: Mango.Expression) =
        async {
            let request = createCustomJsonPost props (sprintf "%s/_find" dbName) [ MangoConverters.OperatorJsonConverter () :> JsonConverter ] expression []
            let! result = sendRequest request
            let queryResult = { QueryResult.content = result.content; QueryResult.statusCode = result.statusCode }

            do printfn "Response content:%s%s" System.Environment.NewLine queryResult.content

            return match queryResult.statusCode with
                    | Some 200 ->
                        match deserializeJsonWith<Response<'a>> [] queryResult.content with
                        | Ok o -> Success o
                        | Error e -> JsonDeserializationError <| RequestResult.createForJson(e, result.statusCode, result.headers)
                    | Some 400 -> BadRequest result
                    | Some 401 -> Unauthorized result
                    | Some 500 -> QueryExecutionError result
                    | _ ->
                        Unknown result
        }
        // CouchDb contains a syntax to define the fields to return but since we are using Json-deserialization
        // this is currently not in use.
        
    /// Returns the result from the query as a generic `FSharp.Core.Result`.
    let asResult<'a> (r: Result<'a>) =
        match r with
        | Success x -> Ok x
        | BadRequest e | Unauthorized e | QueryExecutionError e | JsonDeserializationError e | Unknown e ->
            Error <| ErrorRequestResult.fromRequestResultAndCase(e, r)
            
    let queryAsResult<'a> props dbName expression = query<'a> props dbName expression |> Async.map asResult<'a>