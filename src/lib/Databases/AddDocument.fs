namespace b0wter.CouchDb.Lib.Database

//
// Queries: /{db} [POST]
//

open b0wter.CouchDb.Lib
open b0wter.CouchDb.Lib.Core
open b0wter.FSharp
open Newtonsoft.Json
open b0wter.CouchDb.Lib
open b0wter.CouchDb.Lib

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
        | Failure of RequestResult.T

    let query (props: DbProperties.T) (dbName: string) (obj: obj) =
        async {
            let request = createJsonPost props dbName obj []
            let! result = sendRequest request 
            match result.statusCode with
            | Some 201 | Some 202 ->
                try
                    // TODO: remove the explicit JsonConvert and use something from Core
                    let response = JsonConvert.DeserializeObject<Response>(result.content)
                    return Created response
                with
                | :? JsonException as ex ->
                    return Failure <| RequestResult.createWithHeaders (None, ex.Message, result.headers)
            | Some 400 -> return InvalidDatabaseName
            | Some 401 -> return Unauthorized
            | Some 404 -> return DatabaseDoesNotExist
            | Some 409 -> return DocumentIdConflict
            | x -> return Failure <| RequestResult.createWithHeaders (x, result.content, result.headers)
        }
