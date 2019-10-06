namespace b0wter.CouchDb.Lib.Databases

//
// Queries: /{db} [POST]
//

open b0wter.CouchDb.Lib
open b0wter.CouchDb.Lib.Core
open b0wter.FSharp

module AddDocument =
    type Response = {
        id: string
        ok: bool
        rev: string
    }

    type Result
        = Created of Response
        | Accepted of Response
        | InvalidDbName of RequestResult.T
        | Unauthorized of RequestResult.T
        | DbDoesNotExist of RequestResult.T
        | DocumentIdConflict of RequestResult.T
        | JsonDeserializationError of RequestResult.T
        | Unknown of RequestResult.T

    let query (props: DbProperties.T) (dbName: string) (obj: obj) =
        async {
            let request = createJsonPost props dbName obj []
            let! result = sendRequest request 
            match result.statusCode with
            | Some 201 | Some 202 ->
                return match deserializeJson<Response> result.content with
                        | Ok o -> Created o
                        | Error e -> JsonDeserializationError <| RequestResult.createForJson(e, result.statusCode, result.headers)
            | Some 400 -> return InvalidDbName result
            | Some 401 -> return Unauthorized result
            | Some 404 -> return DbDoesNotExist result
            | Some 409 -> return DocumentIdConflict result
            | x -> return Unknown <| RequestResult.createWithHeaders (x, result.content, result.headers)
        }

    /// Returns the result from the query as a generic `FSharp.Core.Result`.
    let asResult (r: Result) =
        match r with
        | Created x | Accepted x -> Ok x
        | InvalidDbName e | Unauthorized e | DbDoesNotExist e | DocumentIdConflict e | JsonDeserializationError e | Unknown e ->
            Error <| ErrorRequestResult.fromRequestResultAndCase(e, r)
            
    let queryAsResult props dbName obj = query props dbName obj |> Async.map asResult
