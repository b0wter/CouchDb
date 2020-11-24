namespace b0wter.CouchDb.Lib.DesignDocuments

module Get =

    open b0wter.CouchDb.Lib
    open b0wter.FSharp

    type Result = HttpVerbs.Get.Result<DesignDocument.DesignDocument>

    type Response<'a> = HttpVerbs.Get.Response<'a>

    let private designDocumentConverters = [ DesignDocuments.Converter.DesignDocumentConverter() :> Newtonsoft.Json.JsonConverter ]

    let query dbProps dbName id queryParameters =
        async {
            if System.String.IsNullOrWhiteSpace(dbName) then
                return Result.DbNameMissing <| RequestResult.createText (None, "The database name is empty. The query has not been sent to the server.")
            else
                let url = (sprintf "%s/_design/%s" dbName (id |> string))
                return! HttpVerbs.Get.query<DesignDocument.DesignDocument> dbProps url id queryParameters designDocumentConverters
        }

    let asResult = HttpVerbs.Get.asResult

    let queryAsResult dbProps dbName id queryParameters = query dbProps dbName id queryParameters |> Async.map asResult