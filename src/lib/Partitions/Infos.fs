namespace b0wter.CouchDb.Lib.Partitions

//
// Queries: /{db}/partition/{partition} [HEAD]
//

open b0wter.CouchDb.Lib
open b0wter.CouchDb.Lib.Core
open Newtonsoft.Json
open Utilities

module Infos =
    
    type Response = {
        [<JsonProperty("db_name")>]
        DbName: string
        [<JsonProperty("doc_count")>]
        DocumentCount: int
        [<JsonProperty("doc_del_count")>]
        DeletedDocumentCount: int
        Partition: string
        Sizes: {| Active: int; External: int |}
    }
    
    type Result =
        /// Request was successful
        | Success of Response
        /// The database or partition could not be found
        | NotFound of RequestResult.StringRequestResult
        /// Read privilege required
        | Unauthorized of RequestResult.StringRequestResult
        /// You have not supplied a database name, no request has been sent to the server
        | DbNameMissing of RequestResult.StringRequestResult
        /// You have not supplied a partition name, no request has been sent to the server
        | PartitionNameMissing of RequestResult.StringRequestResult
        /// An error occured while deserializing the response from the server
        | JsonDeserializationError of RequestResult.StringRequestResult
        /// Is returned if the response could not be interpreted as a case specified by the documentation.
        | Unknown of RequestResult.StringRequestResult
    
    let query props dbName partitionName =
        async {
            if System.String.IsNullOrWhiteSpace(dbName) then
                return Result.DbNameMissing <| RequestResult.createText (None, "The database name is empty. The query has not been sent to the server.")
            else if System.String.IsNullOrWhiteSpace(partitionName) then
                return Result.PartitionNameMissing <| RequestResult.createText (None, "The database name is empty. The query has not been sent to the server.")
            else
                let url = sprintf "/%s/_partition/%s" dbName partitionName
                let request = createGet props url []
                let! result = sendTextRequest request
                
                return match result.StatusCode with
                       | Some 200 ->
                         let document = result.Content |> deserializeJson<Response>
                         match document with
                         | Ok d -> Success d
                         | Error e -> JsonDeserializationError <| RequestResult.createForJson(e, result.StatusCode, result.Headers)
                       | Some 401 -> Unauthorized result
                       | Some 404 -> NotFound result
                       | _        -> Unknown result
        }

    /// Maps the query specific `Result` to a generic `FSharp.Core.Result`
    let asResult (r: Result) =
        match r with
        | Success response -> Ok response
        | NotFound e | Unauthorized e | DbNameMissing e | PartitionNameMissing e | JsonDeserializationError e | Unknown e -> Error e

    /// Runs `query` followed by `asResult`
    let queryAsResult props dbName partitionName = query props dbName partitionName |> Async.map asResult