namespace b0wter.CouchDb.Lib.Databases
open b0wter.CouchDb.Lib
open Utilities

module BulkUpdate =
    
    type Result
        /// Document(s) have been created or updated (201)
        = Created of BulkAdd.Response
        /// The request provided invalid JSON data (400)
        | BadRequest of RequestResult.StringRequestResult
        /// Occurs when at least one document was rejected by a validation function (417)
        | ExpectationFailed of RequestResult.StringRequestResult
        /// Occurs when the local deserialization of a response failed.
        | JsonDeserializationError of RequestResult.StringRequestResult
        /// Occurs of the database name is null or empty. No request has been sent to the server.
        | DbNameMissing of RequestResult.StringRequestResult
        /// Occurs if response could not be interpreted.
        | Unknown of RequestResult.StringRequestResult
        /// Returned if at least one of the documents failed the `idSetCheck`.
        | IdCheckFailed of RequestResult.StringRequestResult
        /// Returned if at least one of the documents failed the `revSetCheck`.
        | RevCheckFailed of RequestResult.StringRequestResult
        /// Requested database does not exist
        | NotFound of RequestResult.StringRequestResult
    
    /// This query wraps `BulkAdd.query` but requires that each doc passes the given idSetCheck and revSetCheck.
    /// This makes sure that the documents are updates not insertions.
    let query (props: DbProperties.DbProperties) (dbName: string) (idSetCheck: 'a -> bool) (revSetCheck: 'a -> bool) (docs: 'a list) =
        async {
            if docs |> List.forall idSetCheck |> not then
                return IdCheckFailed <| RequestResult.createText (None, "The idSetCheck failed for at least one document. No request has been sent to the server.")
            else if docs |> List.forall revSetCheck |> not then
                return RevCheckFailed <| RequestResult.createText (None, "The revSetCheck failed for at least one document. No request has been sent to the server.")
            else
                let! result = BulkAdd.query props dbName docs
                return match result with
                        | BulkAdd.Result.Created x -> Created x
                        | BulkAdd.Result.BadRequest x -> BadRequest x
                        | BulkAdd.Result.ExpectationFailed x -> ExpectationFailed x
                        | BulkAdd.Result.JsonDeserializationError x -> JsonDeserializationError x
                        | BulkAdd.Result.DbNameMissing x -> DbNameMissing x
                        | BulkAdd.Result.NotFound x -> NotFound x
                        | BulkAdd.Result.Unknown x -> Unknown x
        }
        
    /// Returns the result from the query as a generic `FSharp.Core.Result`.
    let asResult (r: Result) =
        match r with
        | Created r -> Ok r
        | BadRequest e | ExpectationFailed e | JsonDeserializationError e | DbNameMissing e | Unknown e | IdCheckFailed e | RevCheckFailed e | NotFound e -> Error <| ErrorRequestResult.fromRequestResultAndCase (e, r)
        
    /// Runs query followed by asResult.
    let queryAsResult props dbName idSetCheck revSetCheck docs = query props dbName idSetCheck revSetCheck docs |> Async.map asResult
