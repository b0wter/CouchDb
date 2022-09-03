namespace b0wter.CouchDb.Lib.Documents

module Put =

    open b0wter.CouchDb.Lib
    open b0wter.FSharp

    type Result = HttpVerbs.Put.Result

    type Response = HttpVerbs.Put.Response

    let query<'a> dbProps dbName docId docRev document =
        async {
            if System.String.IsNullOrWhiteSpace(dbName) then
                return Result.DbNameMissing <| RequestResult.createText (None, "The database name is empty. The query has not been sent to the server.")
            else
                let url = sprintf "%s/%s" dbName (document |> docId |> string)
                return! HttpVerbs.Put.query<'a> dbProps url [] docId docRev document
        }

    let queryAsResult<'a> dbProps dbName docId docRev document = query<'a> dbProps dbName docId docRev document |> Async.map HttpVerbs.Put.asResult

    let asResult = HttpVerbs.Put.asResult