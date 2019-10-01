namespace b0wter.CouchDb.Lib.Database

//
// Queries: /{db} [POST]
//

open b0wter.CouchDb.Lib
open b0wter.CouchDb.Lib.Core
open b0wter.FSharp
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
            let! result = sendRequest request |> Async.map (fun x -> x :> IRequestResult)
            match result.StatusCode with
            | Some 201 | Some 202 ->
                try
                    // TODO: remove the explicit JsonConvert and use something from Core
                    let response = JsonConvert.DeserializeObject<Response>(result.Body)
                    return Created response
                with
                | :? JsonException as ex ->
                    return Failure <| errorRequestResult (None, ex.Message, Some result.Headers)
            | Some 400 -> return InvalidDatabaseName
            | Some 401 -> return Unauthorized
            | Some 404 -> return DatabaseDoesNotExist
            | Some 409 -> return DocumentIdConflict
            | x -> return Failure <| errorRequestResult (x, result.Body, Some result.Headers)
        }
