namespace b0wter.CouchDb.Lib.DesignDoc

module Put =

    open b0wter.CouchDb.Lib
    open b0wter.FSharp

    type Result = HttpVerbs.Put.Result

    let query<'a> dbProps dbName docId docRev document =
        async {
            if System.String.IsNullOrWhiteSpace(dbName) then
                return Result.DbNameMissing <| RequestResult.create (None, "The database name is empty. The query has not been sent to the server.")
            else
                let url = (sprintf "%s/_design/%s" dbName (document |> docId |> string)) 
                return! HttpVerbs.Put.query<'a> dbProps url docId docRev document
        }

    let queryAsResult dbProps dbName docId docRev document = query dbProps dbName docId docRev document |> Async.map HttpVerbs.Put.asResult