
namespace b0wter.CouchDb.Lib.DesignDoc

module Get =

    open b0wter.CouchDb.Lib
    open b0wter.FSharp

    type Result<'a> = HttpVerbs.Get.Result<'a>

    let query<'a> dbProps dbName id queryParameters =
        async {
            if System.String.IsNullOrWhiteSpace(dbName) then
                return Result<'a>.DbNameMissing <| RequestResult.create (None, "The database name is empty. The query has not been sent to the server.")
            else
                let url = (sprintf "%s/_design/%s" dbName (id |> string))
                return! HttpVerbs.Get.query<'a> dbProps dbName url id queryParameters
        }

    let queryAsResult dbProps dbName id queryParameters = query dbProps dbName id queryParameters |> Async.map HttpVerbs.Get.asResult
