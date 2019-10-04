namespace b0wter.CouchDb.Lib.Database

//
// Queries: /{db} [HEAD]
//

open b0wter.CouchDb.Lib
open b0wter.CouchDb.Lib.Core
open b0wter.FSharp.Operators

module Exists =

    type Result
        = Exists
        | DoesNotExist
        | DbNameMissing
        | Unknown of RequestResult.T

    let query (props: DbProperties.T) (name: string) : Async<Result> =
        async {
            if System.String.IsNullOrWhiteSpace(name) then return DbNameMissing else
            let request = createHead props name []
            let! result = sendRequest request
            return match result.statusCode with
                    | Some 200 -> Exists
                    | Some 404 -> DoesNotExist
                    | _ -> Unknown result
        }
