namespace b0wter.CouchDb.Lib.Server

    //
    // Queries: /_all_dbs
    //
    
    open Newtonsoft.Json
    open b0wter.CouchDb.Lib.Core
    open b0wter.CouchDb.Lib

    module AllDbs =
        type Response = string list

        type Result
            = Success of Response
            | JsonDeserialisationError of JsonDeserialisationError
            | Unknown of RequestResult.T

        /// <summary>
        /// Returns a list of strings containing the names of all databases.
        /// </summary>
        let query (props: DbProperties.T) : Async<Result> =
            async {
                let request = createGet props "_all_dbs" []
                let! result = sendRequest request
                return match result.statusCode with
                        | Some 200 -> match deserializeJson<Response> result.content with
                                      | Ok response -> Success response
                                      | Error r -> JsonDeserialisationError r
                        | _ -> Unknown result
            }

