namespace b0wter.CouchDb.Lib.DesignDocuments

module Delete =

    open b0wter.CouchDb.Lib
    open b0wter.FSharp

    type Result = HttpVerbs.Delete.Result

    let query<'a> dbProps dbName docId (docRev: string) =
        async {
            if System.String.IsNullOrWhiteSpace(dbName) then 
                return Result.DbNameMissing <| RequestResult.create (None, "The database name is empty. The query has not been sent to the server.")
            else
                let url = sprintf "%s/_design/%s" dbName (docId |> string)
                return! HttpVerbs.Delete.query<'a> dbProps url docId docRev
        }

    let queryAsResult dbProps dbName docId (docRev: string) = query dbProps dbName docId docRev |> Async.map HttpVerbs.Delete.asResult
