namespace b0wter.CouchDb.Lib.Databases

module BulkDelete =
    
    open b0wter.CouchDb.Lib
    open b0wter.FSharp
    
    type Result
        /// Document(s) have been created or updated (201). *Beware*, CouchDb returns created
        /// even if the deletion of some documents failed. You need to check the InsertResult.
        /// Because delete means that documents are
        /// updated with `_deleted=true` this case is still named `Created` in accordance with
        /// BulkAdd and BulkUpdate.
        = Created of BulkAdd.Response
        /// The request provided invalid JSON data (400)
        | BadRequest of RequestResult.T
        /// Occurs when at least one document was rejected by a validation function (417)
        | ExpectationFailed of RequestResult.T
        /// Occurs when the local deserialization of a response failed.
        | JsonDeserializationError of RequestResult.T
        /// Occurs of the database name is null or empty. No request has been sent to the server.
        | DbNameMissing of RequestResult.T
        /// Occurs if response could not be interpreted.
        | Unknown of RequestResult.T
        /// Returned if at least one of the documents failed the `idSetCheck`.
        | IdCheckFailed of RequestResult.T
        /// Returned if at least one of the documents failed the `revSetCheck`.
        | RevCheckFailed of RequestResult.T
        /// Requested database does not exist
        | NotFound of RequestResult.T
    
    type DeleteUpdate<'a, 'b>(id: 'a, rev: 'b) = 
        member this._id = id
        member this._rev = rev
        member this._deleted = true
        
    /// This query wraps `BulkAdd.query` but requires that each doc passes the given idSetCheck and revSetCheck.
    /// This makes sure that the documents are updates not insertions.
    let query (props: DbProperties.T) (dbName: string) (isIdValid: 'a -> bool) (isRevValid: 'b -> bool) (idsAndRevs: ('a * 'b) list) =
        async {
            let ids = idsAndRevs |> List.map fst
            let revs = idsAndRevs |> List.map snd
            if ids |> List.exists (not << isIdValid) then
                return IdCheckFailed <| RequestResult.create (None, "At least one document id failed the validation. No request has been sent to the server.")
            else if revs |> List.exists (not << isRevValid) then
                return RevCheckFailed <| RequestResult.create (None, "At least one document rev is empty. No request has been sent to the server.")
            else
                let deleteUpdates = idsAndRevs |> List.map DeleteUpdate
                let! result = BulkAdd.query props dbName deleteUpdates
                return match result with
                        | BulkAdd.Result.Created x -> Created x
                        | BulkAdd.Result.BadRequest x -> BadRequest x
                        | BulkAdd.Result.ExpectationFailed x -> ExpectationFailed x
                        | BulkAdd.Result.JsonDeserializationError x -> JsonDeserializationError x
                        | BulkAdd.Result.DbNameMissing x -> DbNameMissing x
                        | BulkAdd.Result.NotFound x -> NotFound x
                        | BulkAdd.Result.Unknown x -> Unknown x
        }
        
    /// Works like `query` but uses full documents instead of a `(id * rev) list`.
    let queryWithDocs props dbName isIdValid isRevValid (getId: 'a ->'b) (getRev: 'a -> 'c) (docs: 'a list) =
        query props dbName isIdValid isRevValid (docs |> List.map (fun d -> (d |> getId, d |> getRev)))
        
    /// Returns the result from the query as a generic `FSharp.Core.Result`.
    let asResult (r: Result) =
        match r with
        | Created r -> Ok r
        | BadRequest e | ExpectationFailed e | JsonDeserializationError e | DbNameMissing e | Unknown e | IdCheckFailed e | RevCheckFailed e | NotFound e -> Error <| ErrorRequestResult.fromRequestResultAndCase (e, r)
        
    /// Runs query followed by asResult.
    let queryAsResult props dbName isIdValid isRefValid idsAndRevs = query props dbName isIdValid isRefValid idsAndRevs |> Async.map asResult
    
    /// Runs queryWithChheck followed by asResult.
    let queryWithDocsAsResult props dbName getid getRev isValidId isValidRef docs = queryWithDocs props dbName getid getRev isValidId isValidRef docs |> Async.map asResult

