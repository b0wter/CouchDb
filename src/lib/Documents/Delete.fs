namespace b0wter.CouchDb.Lib.Documents

//
// Queries: /{db}/{docid} [DELETE]
//

open Newtonsoft.Json
open b0wter.CouchDb.Lib
open b0wter.CouchDb.Lib.Core
open b0wter.CouchDb.Lib.QueryParameters
open b0wter.FSharp

module Delete =

    type Response = {
        id: System.Guid
        ok: bool
        rev: string
    }
    
    type Result
        /// Json deserialization failed
        = JsonDeserialisationError of ErrorRequestResult
        /// Document id is empty
        | DocumentIdEmpty
        /// Document rev is empty
        | DocumentRevEmpty
        /// Document successfully removed (200)
        | Ok of Response
        /// Request was accepted, but changes are not yet stored on disk (202)
        | Accepted of Response 
        /// Invalid request body or parameters (400)
        | BadRequest of ErrorRequestResult 
        /// Write privileges required (401)
        | Unauthorized of ErrorRequestResult
        /// Specified database or document ID doesn’t exists (404)
        | NotFound of ErrorRequestResult
        /// Specified revision is not the latest for target document (409)
        | Conflict of ErrorRequestResult
        /// If the result could not be interpreted.
        | Unknown of ErrorRequestResult
    
    /// Marks the specified document as deleted by adding a field _deleted with the value true.
    /// Documents with this field will not be returned within requests anymore, but stay in the database.
    /// You must supply the current (latest) revision, either by using the rev parameter or by using
    /// the If-Match header to specify the revision.
    let query<'a> (props: DbProperties.T) (dbName: string) (docId: System.Guid) (docRev: string) : Async<Result> =
        async {
            if docId = System.Guid.Empty then
                return DocumentIdEmpty
            else if System.String.IsNullOrWhiteSpace(docRev) then
                return DocumentRevEmpty
            else
                let queryParams = [ StringQueryParameter("rev", docRev) :> BaseQueryParameter ]
                let url = sprintf "%s/%s" dbName (docId |> string)
                let request = createDelete props url queryParams
                let! result = sendRequest request
                let iresult = result :> IRequestResult
                return match iresult.StatusCode with
                        | Some 200 ->
                            match deserializeJson [] iresult.Body with
                            | FSharp.Core.Result.Ok response -> Result.Ok response
                            | Error e -> JsonDeserialisationError <| errorRequestResult(iresult.StatusCode, sprintf "Reason: %s%sJson:%s" e.reason System.Environment.NewLine e.json, Some iresult.Headers)
                        | Some 202 ->
                            match deserializeJson [] iresult.Body with
                            | FSharp.Core.Result.Ok response -> Accepted response
                            | Error e -> JsonDeserialisationError <| errorRequestResult(iresult.StatusCode, sprintf "Reason: %s%sJson:%s" e.reason System.Environment.NewLine e.json, Some iresult.Headers)
                        | Some 400 -> BadRequest <| errorFromIRequestResult iresult
                        | Some 401 -> Unauthorized <| errorFromIRequestResult iresult
                        | Some 404 -> NotFound <| errorFromIRequestResult iresult
                        | Some 409 -> Conflict <| errorFromIRequestResult iresult
                        | _ -> Unknown <| errorFromIRequestResult iresult
        }
