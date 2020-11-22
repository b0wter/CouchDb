namespace b0wter.CouchDb.Lib.HttpVerbs

//
// Queries: /{db}/{docid} [DELETE]
//

open b0wter.CouchDb.Lib
open b0wter.CouchDb.Lib.Core
open b0wter.CouchDb.Lib.QueryParameters
open b0wter.FSharp

module Delete =

    type Response = Databases.BulkAdd.Success
    
    type Result
        /// Document successfully removed (200)
        = Ok of Response
        /// Request was accepted, but changes are not yet stored on disk (202)
        | Accepted of Response 
        /// Json deserialization failed
        | JsonDeserialisationError of RequestResult.TString
        /// Document id is empty
        | DocumentIdEmpty of RequestResult.TString
        /// Document rev is empty
        | DocumentRevEmpty of RequestResult.TString
        /// The database name is empty.
        | DbNameMissing of RequestResult.TString
        /// Invalid request body or parameters (400)
        | BadRequest of RequestResult.TString 
        /// Write privileges required (401)
        | Unauthorized of RequestResult.TString
        /// Specified database or document ID doesn’t exists (404)
        | NotFound of RequestResult.TString
        /// Specified revision is not the latest for target document (409)
        | Conflict of RequestResult.TString
        /// If the result could not be interpreted.
        | Unknown of RequestResult.TString
    
    /// Marks the specified document as deleted by adding a field _deleted with the value true.
    /// Documents with this field will not be returned within requests anymore, but stay in the database.
    /// You must supply the current (latest) revision, either by using the rev parameter or by using
    /// the If-Match header to specify the revision.
    let query<'a> (props: DbProperties.T) (url: string) (docId: string) (docRev: string) : Async<Result> =
        async {
            if docId |> String.isNullOrWhiteSpace then
                return DocumentIdEmpty <| RequestResult.create(None, "You need to supply a non-empty document id. The query has not been sent to the server.")
            else if System.String.IsNullOrWhiteSpace(docRev) then
                return DocumentRevEmpty <| RequestResult.create(None, "You need to supply a non-empty document rev. The query has not been sent to the server.")
            else
                let queryParams = [ StringQueryParameter("rev", docRev) :> BaseQueryParameter ]
                let request = createDelete props url queryParams
                let! result = sendRequest request
                return match result.StatusCode with
                        | Some 200 ->
                            match deserializeJsonWith [] result.Content with
                            | FSharp.Core.Result.Ok response -> Result.Ok response
                            | Error e -> JsonDeserialisationError <| RequestResult.createWithHeaders (result.StatusCode, sprintf "Reason: %s%sJson:%s" e.Reason System.Environment.NewLine e.Json, result.Headers)
                        | Some 202 ->
                            match deserializeJsonWith [] result.Content with
                            | FSharp.Core.Result.Ok response -> Accepted response
                            | Error e -> JsonDeserialisationError <| RequestResult.createWithHeaders (result.StatusCode, sprintf "Reason: %s%sJson:%s" e.Reason System.Environment.NewLine e.Json, result.Headers)
                        | Some 400 -> BadRequest result
                        | Some 401 -> Unauthorized result
                        | Some 404 -> NotFound result
                        | Some 409 -> Conflict result
                        | _ -> Unknown result
        }

    /// Returns the result from the query as a generic `FSharp.Core.Result`.
    let asResult (r: Result) =
        match r with
        | Ok x | Accepted x -> FSharp.Core.Result.Ok x
        | BadRequest e | DbNameMissing e | NotFound e | Unauthorized e | Conflict e | DocumentIdEmpty e | DocumentRevEmpty e | JsonDeserialisationError e | Unknown e ->
            FSharp.Core.Result.Error <| ErrorRequestResult.fromRequestResultAndCase(e, r)
            
    /// Runs query followed by asResult.
    let queryAsResult<'a> props url docId docRev = query<'a> props url docId docRev |> Async.map asResult