namespace b0wter.CouchDb.Lib.Documents


module Get =

    open Newtonsoft.Json.Linq
    open b0wter.CouchDb.Lib
    open b0wter.FSharp

    type Result<'a> = HttpVerbs.Get.Result<'a>

    type Response<'a> = HttpVerbs.Get.Response<'a>

    /// <summary>
    /// Queries the database for a single document using a document id and an optional revision.
    /// If no revision is supplied the latest version is returned. The document is deserialized into 'a using
    /// the custom converters.
    /// </summary>
    let queryWith<'a> dbProps dbName id queryParameters customConverters =
        async {
            if System.String.IsNullOrWhiteSpace(dbName) then
                return Result<'a>.DbNameMissing <| RequestResult.createText (None, "The database name is empty. The query has not been sent to the server.")
            else
                let url = (sprintf "%s/%s" dbName (id |> string))
                return! HttpVerbs.Get.query<'a> dbProps url id queryParameters customConverters
        }
        
    /// <summary>
    /// Queries the database for a single document using a document id and an optional revision.
    /// If no revision is supplied the latest version is returned. The document is returned as a `JObject`.
    /// </summary>
    let queryJObject dbProps dbName id queryParameters =
        async {
            if System.String.IsNullOrWhiteSpace(dbName) then
                return Result<JObject>.DbNameMissing <| RequestResult.createText (None, "The database name is empty. The query has not been sent to the server.")
            else
                let url = (sprintf "%s/%s" dbName (id |> string))
                return! HttpVerbs.Get.queryJObject dbProps url id queryParameters
        }

    /// <summary>
    /// Identical to `queryWith` but does not use any custom converter.
    /// </summary>
    let query<'a> dbProps dbName id queryParameters = queryWith<'a> dbProps dbName id queryParameters []
    
    /// <summary>
    /// Runs `queryWith` followed by `asResult`.
    /// </summary>
    let queryAsResultWith<'a> dbProps dbName id queryParameters customConverters = queryWith<'a> dbProps dbName id queryParameters customConverters |> Async.map HttpVerbs.Get.asResult

    /// <summary>
    /// Runs `query` followed by `asResult`.
    /// </summary>
    let queryAsResult<'a> dbProps dbName id queryParameters = query<'a> dbProps dbName id queryParameters |> Async.map HttpVerbs.Get.asResult
    
    /// <summary>
    /// Runs `queryJObject` followed by `asResult`.
    /// </summary>
    let queryJObjectAsResult dbProps dbName id queryParameters = queryJObject dbProps dbName id queryParameters |> Async.map HttpVerbs.Get.asResult

    /// <summary>
    /// Converts the `Get.Result` into a regular `FSharp.Result`.
    /// </summary>
    let asResult = HttpVerbs.Get.asResult
    