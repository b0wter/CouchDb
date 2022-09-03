namespace b0wter.CouchDb.Lib.Attachments

module Head =

    open b0wter.CouchDb.Lib
    open Utilities

    type Result = HttpVerbs.Head.Result

    type Response = HttpVerbs.Head.Response

    let query dbProps dbName documentId attachmentName (revision: string option) =
        async {
            if System.String.IsNullOrWhiteSpace(dbName) then
                return Result.DbNameMissing <| RequestResult.createText (None, "The database name is empty. The query has not been sent to the server.")
            else if System.String.IsNullOrWhiteSpace(attachmentName) then
                return Result.ParameterIsMissing <| RequestResult.createText (None, "The attachmentName is empty. The query has not been sent to the server.")
            else
                let url = sprintf "%s/%s/%s" dbName documentId attachmentName
                let url = match revision with Some rev -> url + (sprintf "?rev=%s" (System.Web.HttpUtility.UrlEncode(rev))) | None -> url
                return! HttpVerbs.Head.query dbProps url documentId
        }

    let queryAsResult dbProps dbName documentId attachmentName revision = query dbProps dbName documentId attachmentName revision |> Async.map HttpVerbs.Head.asResult

    let asResult = HttpVerbs.Head.asResult