namespace b0wter.CouchDb.Lib.Documents

module Head =

    open b0wter.CouchDb.Lib
    open b0wter.FSharp

    type Result = HttpVerbs.Head.Result

    type Response = HttpVerbs.Head.Response

    let query dbProps dbName id =
        async {
            if System.String.IsNullOrWhiteSpace(dbName) then
                return Result.DbNameMissing <| RequestResult.create (None, "The database name is empty. The query has not been sent to the server.")
            else
                let url =  (sprintf "%s/%s" dbName (id |> string))
                return! HttpVerbs.Head.query dbProps url id
        }

    let queryAsResult dbProps dbName id = query dbProps dbName id |> Async.map HttpVerbs.Head.asResult

    let asResult = HttpVerbs.Head.asResult