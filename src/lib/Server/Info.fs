namespace b0wter.CouchDb.Lib.Server

    //
    // Queries: /
    //
    
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
            | JsonDeserialisationError of JsonDeserialisationError
            | Unknown of RequestResult.T

        let query (props: DbProperties.T) : Async<Result> =
            async {
                let request = createGet props "/" []
                let! result = sendRequest request
                return match result.statusCode with
                        | Some 200 -> match deserializeJson<Response> result.content with
                                      | Ok response -> Success response
                                      | Error r -> JsonDeserialisationError r
                        | _ -> Unknown result
            }

