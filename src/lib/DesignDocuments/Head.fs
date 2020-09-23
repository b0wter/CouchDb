namespace b0wter.CouchDb.Lib.DesignDocuments

module Head =

    open b0wter.CouchDb.Lib
    open b0wter.FSharp

    type Result = HttpVerbs.Head.Result

    let query dbProps dbName id =
        async {
            if System.String.IsNullOrWhiteSpace(dbName) then
                return Result.DbNameMissing <| RequestResult.create (None, "The database name is empty. The query has not been sent to the server.")
            else
                let url = (sprintf "%s/_design/%s" dbName (id |> string))
                return! HttpVerbs.Head.query dbProps url id
        }

    let queryAsResult dbProps dbName id = query dbProps dbName id |> Async.map HttpVerbs.Head.asResult
