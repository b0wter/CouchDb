namespace b0wter.CouchDb.Lib.Database

//
// Queries: /{db}/_find [POST]
//

open b0wter.CouchDb.Lib
open b0wter.CouchDb.Lib.Core
open Newtonsoft.Json

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
        


