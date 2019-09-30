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
                | Ok o ->
                    do printfn "%s" o.content
                    try
                        return Success <| JsonConvert.DeserializeObject<Response>(o.content)
                    with
                    | :? JsonException as ex -> return Failure <| errorRequestResult (0, ex.Message)
                | Error e ->
                    return Failure e
            }

