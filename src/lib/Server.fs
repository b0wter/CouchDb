namespace b0wter.CouchDb.Lib

module Server =

    open Newtonsoft.Json

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
            | Failure of Core.ErrorRequestResult

        let query (props: DbProperties.T) : Async<Result> =
            async {
                let request = Core.createGet props "/"
                match! Core.sendRequest props request with
                | Ok o ->
                    do printfn "%s" o.content
                    try
                        return Success <| JsonConvert.DeserializeObject<Response>(o.content)
                    with
                    | :? JsonException as ex -> return Failure <| Core.errorRequestResult (0, ex.Message)
                | Error e ->
                    return Failure e
            }
        