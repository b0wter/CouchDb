namespace b0wter.CouchDb.Lib.DesignDocuments

module Put =

    open b0wter.CouchDb.Lib
    open b0wter.FSharp

    type Result = HttpVerbs.Put.Result

    type Response = HttpVerbs.Put.Response

    let private designDocumentId (d: DesignDocument.DesignDocument) = d.Id
    let private designDocumentRev (d: DesignDocument.DesignDocument) = d.Rev

    let query<'a> dbProps dbName (document: DesignDocument.DesignDocument) =
        async {
            if System.String.IsNullOrWhiteSpace(dbName) then
                return Result.DbNameMissing <| RequestResult.createText (None, "The database name is empty. The query has not been sent to the server.")
            else
                let url = (sprintf "%s/_design/%s" dbName (document |> designDocumentId |> string)) 
                return! HttpVerbs.Put.query<DesignDocument.DesignDocument> dbProps url [ Converter.DesignDocumentConverter() ] designDocumentId designDocumentRev document
        }

    let asResult = HttpVerbs.Put.asResult

    let queryAsResult dbProps dbName document = query dbProps dbName document |> Async.map HttpVerbs.Put.asResult