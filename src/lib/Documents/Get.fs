namespace b0wter.CouchDb.Lib.Documents

module Get =

    open b0wter.CouchDb.Lib
    open b0wter.FSharp

    type Result<'a> = HttpVerbs.Get.Result<'a>

    type Response<'a> = HttpVerbs.Get.Response<'a>

    let queryWith<'a> dbProps dbName id queryParameters customConverters =
        async {
            if System.String.IsNullOrWhiteSpace(dbName) then
                return Result<'a>.DbNameMissing <| RequestResult.createText (None, "The database name is empty. The query has not been sent to the server.")
            else
                let url = (sprintf "%s/%s" dbName (id |> string))
                return! HttpVerbs.Get.query<'a> dbProps url id queryParameters customConverters
        }

    let query<'a> dbProps dbName id queryParameters = queryWith<'a> dbProps dbName id queryParameters []

    let queryAsResultWith dbProps dbName id queryParameters customConverters = queryWith dbProps dbName id queryParameters customConverters |> Async.map HttpVerbs.Get.asResult

    let queryAsResult dbProps dbName id queryParameters = query dbProps dbName id queryParameters |> Async.map HttpVerbs.Get.asResult

    let asResult = HttpVerbs.Get.asResult