namespace b0wter.CouchDb.Lib.Databases
open Newtonsoft.Json
open b0wter.CouchDb.Lib
open b0wter.CouchDb.Lib.Json

module BulkAdd =
    
    open b0wter.FSharp
    open b0wter.CouchDb.Lib.Core
    
    type Success = {
        ok: bool
        id: System.Guid
        rev: string option
    }
    
    type Failure = {
        id: System.Guid option
        error: string
        reason: string
    }
    
    type InsertResult
        = Success of Success
        | Failure of Failure
    
    type Response = InsertResult list
    
    type Result
        /// Document(s) have been created or updated (201)
        = Created of Response
        /// The request provided invalid JSON data (400)
        | BadRequest of RequestResult.T
        /// Occurs when at least one document was rejected by a validation function (417)
        | ExpectationFailed of RequestResult.T
        /// Occurs when the local deserialization of a response failed.
        | JsonDeserializationError of RequestResult.T
        /// Requested database does not exist
        | NotFound of RequestResult.T
        /// Occurs of the database name is null or empty. No request has been sent to the server.
        | DbNameMissing of RequestResult.T
        /// Occurs if response could not be interpreted.
        | Unknown of RequestResult.T
        
    [<RemoveTypeName>]
    type DocumentContainer<'a> = {
        docs: 'a list
    }
    
    type InsertResultConverter() =
        inherit Newtonsoft.Json.JsonConverter()
        override this.CanWrite = false
        
        override this.CanConvert(t) =
            typeof<InsertResult> = t
            
        override this.WriteJson(_, _, _) =
            failwith "This converter does not support writing json."
            
        override this.ReadJson(reader, ``type``, _, serializer) =
            if ``type`` <> typeof<InsertResult> then
                failwith <| sprintf "The converter has been given a non-matching type. Expected `InsertResult` got `%s`" ``type``.FullName
            else
                if reader.TokenType = Newtonsoft.Json.JsonToken.Null then
                    failwith "The given token is null!"
                else
                    let jobject = Newtonsoft.Json.Linq.JObject.Load(reader)
                    if jobject.ContainsKey("id") && jobject.ContainsKey("ok") && jobject.ContainsKey("rev") then
                        jobject.ToObject<Success>(serializer) |> InsertResult.Success :> obj
                    else if jobject.ContainsKey("id") && jobject.ContainsKey("error") && jobject.ContainsKey("reason") then
                        jobject.ToObject<Failure>(serializer) |> InsertResult.Failure :> obj
                    else
                        failwith "The json neither contains all keys necessary to create a `Success` nor a `Failure`."
        
    /// Inserts multiple documents at once. Due to how the B-tree works this is more efficient
    /// than single-document-inserts.
    let query<'a> (props: DbProperties.T) (dbName: string) (docs: 'a list) =
        async {
            if System.String.IsNullOrWhiteSpace(dbName) then
                return DbNameMissing <| RequestResult.create(None, "You need to supply a non-null, non-whitespace database name. No query has been sent to the server.")
            else
                let content = { docs = docs }
                let url = sprintf "%s/_bulk_docs" dbName
                let request = createJsonPost props url content []
                let! result = sendRequest request
                return match result.statusCode with
                       | Some 201 -> match result.content |> deserializeJsonWith<Response> [ InsertResultConverter() :> JsonConverter ] with
                                     | Ok o -> Created o
                                     | Error e -> JsonDeserializationError <| RequestResult.createForJson(e, result.statusCode, result.headers)
                       | Some 400 -> BadRequest result
                       | Some 404 -> NotFound result
                       | Some 417 -> ExpectationFailed result
                       | _ -> Unknown result
        }

    /// Returns the result from the query as a generic `FSharp.Core.Result`.
    let asResult (r: Result) =
        match r with
        | Created r -> Ok r
        | BadRequest e | ExpectationFailed e | JsonDeserializationError e | DbNameMissing e | NotFound e | Unknown e -> Error <| ErrorRequestResult.fromRequestResultAndCase (e, r)
        
    /// Runs query followed by asResult.
    let queryAsResult props dbName expression = query props dbName expression |> Async.map asResult
    
