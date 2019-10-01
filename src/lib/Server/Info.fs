namespace b0wter.CouchDb.Lib.Server

    //
    // Queries: /
    //
    
    open Newtonsoft.Json
    open b0wter.CouchDb.Lib.Core
    open b0wter.CouchDb.Lib

    module Info =
        type VendorInfo = {
            name: string
        }

        type Response = {
            couchdb: string
            uuid: System.Guid
            vendor: VendorInfo
            version: string
            git_sha: string
            features: string list
        }

        type Result
            = Success of Response
            | Failure of ErrorRequestResult

        let query (props: DbProperties.T) : Async<Result> =
            async {
                let request = createGet props "/" []
                match! sendRequest request with
                | SuccessResult s ->
                    do printfn "%s" s.content
                    try
                        return Success <| JsonConvert.DeserializeObject<Response>(s.content)
                    with
                    | :? JsonException as ex -> return Failure <| errorRequestResult (s.statusCode, ex.Message, Some s.headers)
                | ErrorResult e ->
                    return Failure e
            }

