namespace b0wter.CouchDb.Lib.Documents

//
// Queries: /{db}/{docid} [PUT]
//

open Newtonsoft.Json
open b0wter.CouchDb.Lib
open b0wter.CouchDb.Lib.Core
open b0wter.CouchDb.Lib.QueryParameters
open b0wter.FSharp

module Put =

    type Response = {
        id: System.Guid
        ok: bool
        rev: string
    }
    
    type Result
        /// Document created and stored on disk (201)
        = Created of Response
        /// Document data accepted, but not yet stored on disk (202)
        | Accepted of Response
        /// Invalid request body or parameters (400)
        | BadRequest of ErrorRequestResult
        /// Write privileges required (401)
        | Unauthorized of ErrorRequestResult
        /// Specified database or document ID doesn’t exists (404)
        | NotFound of ErrorRequestResult
        /// Document with the specified ID already exists or specified revision is not latest for target document (409)
        | Conflict of ErrorRequestResult
        /// Is returned before querying the db if the database name is empty.
        | DbNameMissing
        /// Json deserialization failed
        | JsonDeserialisationError of ErrorRequestResult
        /// If the result could not be interpreted.
        | Unknown of ErrorRequestResult
        /// This endpoint requires the document id to be set.
        | DocumentIdMissing

    /// The PUT method creates a new named document, or creates a new revision of the existing document. 
    /// Unlike the POST /{db}, you must specify the document ID in the request URL.
    /// When updating an existing document, the current document revision must be included in the document 
    /// (i.e. the request body), as the rev query parameter, or in the If-Match request header.
    let query<'a> (props: DbProperties.T) (dbName: string) (docId: 'a -> System.Guid) (docRev: 'a -> string option) (document: 'a) : Async<Result> =
        async {
            if System.String.IsNullOrWhiteSpace(dbName) then
                return DbNameMissing
            else if document |> docId = System.Guid.Empty then
                return DocumentIdMissing
            else
                let queryParams = match document |> docRev with
                                  | Some rev -> [ StringQueryParameter("rev", rev) :> BaseQueryParameter ]
                                  | None -> []
                let url = (sprintf "%s/%s" dbName (document |> docId |> string)) 
                let request = createJsonPut props url document queryParams
                let! result = sendRequest request
                let iResult = result :> IRequestResult
                return match iResult.StatusCode with
                        | Some 201 ->
                            match deserializeJson [] iResult.Body with
                            | Ok response -> Created response
                            | Error e -> JsonDeserialisationError <| errorRequestResult(iResult.StatusCode, sprintf "Reason: %s%sJson:%s" e.reason System.Environment.NewLine e.json, Some iResult.Headers)
                        | Some 202 ->
                            match deserializeJson [] iResult.Body with
                            | Ok response -> Accepted response
                            | Error e -> JsonDeserialisationError <| errorRequestResult(iResult.StatusCode, sprintf "Reason: %s%sJson:%s" e.reason System.Environment.NewLine e.json, Some iResult.Headers)
                        | Some 400 -> BadRequest <| errorFromIRequestResult iResult
                        | Some 401 -> Unauthorized <| errorFromIRequestResult iResult
                        | Some 404 -> NotFound <| errorFromIRequestResult iResult
                        | Some 409 -> Conflict <| errorFromIRequestResult iResult
                        | _ -> Unknown <| errorFromIRequestResult iResult
        }