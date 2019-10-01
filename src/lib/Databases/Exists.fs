namespace b0wter.CouchDb.Lib.Database

//
// Queries: /{db} [HEAD]
//

open b0wter.CouchDb.Lib
open b0wter.CouchDb.Lib.Core
open b0wter.FSharp.Operators

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
            if System.String.IsNullOrWhiteSpace(name) then return RequestError <| errorRequestResult (None, "You need to set a database name.", None) else
            let request = createHead props name []
            match! sendRequest request with
            | SuccessResult o ->
                let exists = o.statusCode = Some 200
                return if exists then Exists else DoesNotExist
            | ErrorResult e when e.statusCode = Some 404 ->
                return DoesNotExist
            | ErrorResult e ->
                do printfn "Statuscode: %s" (e.statusCode |> Option.map string |?| "<no status code available>")
                return RequestError e
        }
