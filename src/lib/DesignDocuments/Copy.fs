namespace b0wter.CouchDb.Lib.DesignDocuments

module Copy =

    open b0wter.CouchDb.Lib
    open Utilities

    type Result = HttpVerbs.Copy.Result

    type Response = HttpVerbs.Copy.Response

    let query<'a> (props: DbProperties.DbProperties) (dbName: string) (docId: string) (docRev: string option) (destinationId: string) (destinationRev: string option) =
        async {
            if System.String.IsNullOrWhiteSpace(dbName) then 
                return Result.DbNameMissing <| RequestResult.createText (None, "The database name is empty. The query has not been sent to the server.")
            else
                let url = sprintf "%s/_design/%s" dbName (docId |> string)
                return! HttpVerbs.Copy.query<'a> props url docId docRev destinationId destinationRev
        }

    let queryAsResult dbProps dbName docId docRev destinationId destinationRev = query dbProps dbName docId docRev destinationId destinationRev |> Async.map HttpVerbs.Copy.asResult

    let asResult = HttpVerbs.Copy.asResult