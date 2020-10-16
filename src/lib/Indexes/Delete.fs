namespace b0wter.CouchDb.Lib.Indexes

//
// Queries: /{db}/_index [POST]
//

open b0wter.CouchDb.Lib
open b0wter.CouchDb.Lib.Core
open b0wter.FSharp
open Newtonsoft.Json.Linq
open Newtonsoft.Json

module Delete =

    type Response = {
        Ok: bool
    }

    let TrueCreateResult = { Ok = true}
    let FalseCreateResult = { Ok = false}
    
    type Result
        = Deleted of Response
        | MissingDbName of RequestResult.T
        | MissingDesignDocName of RequestResult.T
        | InvalidDesignDocName of RequestResult.T
        | MissingIndexName of RequestResult.T
        | NotFound of RequestResult.T
        | BadRequest of RequestResult.T
        | Unauthorized of RequestResult.T
        | InternalServerError of RequestResult.T
        | Unknown of RequestResult.T

    let query (props: DbProperties.T) (dbName: string) (designDocumentName: string) (indexName: string) : Async<Result> =
        async {
            if String.isNullOrWhiteSpace dbName then 
                return MissingDbName <| RequestResult.create(None, "No query was sent to the server. You supplied an empty db name.")
            else if String.isNullOrWhiteSpace designDocumentName then 
                return MissingDesignDocName <| RequestResult.create(None, "No query was sent to the server. You supplied an empty db name.")
            else if String.isNullOrWhiteSpace indexName then 
                return MissingIndexName <| RequestResult.create(None, "No query was sent to the server. You supplied an empty db name.")
            else if designDocumentName.StartsWith("_design/") then
                return InvalidDesignDocName <| RequestResult.create(None, "The name of the design document stats with '_design/' which is invalid. Please remove the use only the name not the '_design/' prefix. No query has been sent to the server.")
            else
                let url = sprintf "%s/_index/%s/json/%s" dbName designDocumentName indexName
                let request = createDelete props url []
                let! result = sendRequest request
                let r = match result.StatusCode with
                        | Some 200 -> Deleted TrueCreateResult
                        | Some 400 -> BadRequest result
                        | Some 401 -> Unauthorized result
                        | Some 404 -> NotFound result
                        | Some 500 -> InternalServerError result
                        | _   -> Unknown result
                return r
        }
        
    /// Returns the result from the query as a generic `FSharp.Core.Result`.
    let asResult (r: Result) =
        match r with
        | Deleted x -> Ok x
        | MissingDbName e | MissingDesignDocName e | MissingIndexName e | InvalidDesignDocName e | NotFound e | Unauthorized e | InternalServerError e | BadRequest e | Unknown e -> Error <| ErrorRequestResult.fromRequestResultAndCase(e, r)

    /// Runs query followed by asResult.
    let queryAsResult props dbName designDocumentName indexName = query props dbName designDocumentName indexName |> Async.map asResult
