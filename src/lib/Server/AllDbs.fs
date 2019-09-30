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
            | Failure of ErrorRequestResult

        /// <summary>
        /// Returns a list of strings containing the names of all databases.
        /// </summary>
        let query (props: DbProperties.T) : Async<Result> =
            async {
                let request = createGet props "_all_dbs" []
                match! sendRequest request with
                | Ok o ->
                    try
                        return Success <| JsonConvert.DeserializeObject<string list>(o.content)
                    with
                    | :? JsonException as ex -> return Failure <| errorRequestResult (0, ex.Message)
                | Error e ->
                    return Failure e
            }

