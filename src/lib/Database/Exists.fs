namespace b0wter.CouchDb.Lib.Database

//
// Queries: /{db} [HEAD]
//

open b0wter.CouchDb.Lib
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
