namespace b0wter.CouchDb.Lib.Database

//
// Queries: /{db} [POST]
//

open b0wter.CouchDb.Lib
open b0wter.CouchDb.Lib.Core
open Newtonsoft.Json

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
                    // TODO: remove the explicit JsonConvert and use something from Core
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
