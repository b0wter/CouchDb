namespace b0wter.CouchDb.Lib.Attachments

module Delete =

    open b0wter.CouchDb.Lib
    open b0wter.FSharp

    type Result = HttpVerbs.Delete.Result

    type Response = HttpVerbs.Delete.Response

    let query<'a> dbProps dbName (docId: string) (docRev: string) attachmentName =
        async {
            if System.String.IsNullOrWhiteSpace(dbName) then 
                return Result.DbNameMissing <| RequestResult.createText (None, "The database name is empty. The query has not been sent to the server.")
            else if System.String.IsNullOrWhiteSpace(attachmentName) then
                return Result.BadRequest <| RequestResult.createText (None, "The given attachment name is null or empty. The query has not been sent to the server.")
            else
                let url = sprintf "%s/%s/%s" dbName (docId |> string) attachmentName
                return! HttpVerbs.Delete.query<'a> dbProps url docId docRev
        }

    let queryAsResult dbProps dbName docId (docRev: string) attachmentName = query dbProps dbName docId docRev attachmentName |> Async.map HttpVerbs.Delete.asResult

    let asResult = HttpVerbs.Delete.asResult