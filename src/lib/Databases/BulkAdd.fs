namespace b0wter.CouchDb.Lib.Databases
open Newtonsoft.Json
open b0wter.CouchDb.Lib
open b0wter.CouchDb.Lib.Json

module BulkAdd =
    
    open b0wter.FSharp
    open b0wter.CouchDb.Lib.Core
    
    type Success = {
        Ok: bool
        Id: string
        Rev: string 
    }
    
    type Failure = {
        Id: string
        Error: string
        Reason: string
    }

    let failureAsString (f: Failure) =
        sprintf "%s - %s" f.Error f.Reason
    
    type InsertResult
        = Success of Success
        | Failure of Failure
    
    let insertResultId ir = match ir with | Success s -> s.Id | Failure f -> f.Id
    
    type Response = InsertResult list
    
    /// Checks if a `Response` contains one or more `InsertResult.Failure`.
    let allSuccessful (r: Response) = r |> List.exists (fun x -> match x with Success s -> s.Ok = false | Failure _ -> true)
    
    type Result
        /// Document(s) have been created or updated (201)
        = Created of Response
        /// The request provided invalid JSON data (400)
        | BadRequest of RequestResult.StringRequestResult
        /// Occurs when at least one document was rejected by a validation function (417)
        | ExpectationFailed of RequestResult.StringRequestResult
        /// Occurs when the local deserialization of a response failed.
        | JsonDeserializationError of RequestResult.StringRequestResult
        /// Requested database does not exist
        | NotFound of RequestResult.StringRequestResult
        /// Occurs of the database name is null or empty. No request has been sent to the server.
        | DbNameMissing of RequestResult.StringRequestResult
        /// Occurs if response could not be interpreted.
        | Unknown of RequestResult.StringRequestResult
        
    type DocumentContainer<'a> = {
        Docs: 'a list
    }
    
    type InsertResultConverter() =
        inherit JsonConverter()
        override this.CanWrite = false
        
        override this.CanConvert(t) =
            typeof<InsertResult> = t
            
        override this.WriteJson(_, _, _) =
            failwith "This converter does not support writing json."
            
        override this.ReadJson(reader, ``type``, _, serializer) =
            if ``type`` <> typeof<InsertResult> then
                failwith <| sprintf "The converter has been given a non-matching type. Expected `InsertResult` got `%s`" ``type``.FullName
            else
                if reader.TokenType = JsonToken.Null then
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
    let query<'a> (props: DbProperties.DbProperties) (dbName: string) (docs: 'a list) =
        async {
            if System.String.IsNullOrWhiteSpace(dbName) then
                return DbNameMissing <| RequestResult.createText(None, "You need to supply a non-null, non-whitespace database name. No query has been sent to the server.")
            else
                let content = { Docs = docs }
                let url = sprintf "%s/_bulk_docs" dbName
                let request = createJsonPost props url content []
                let! result = sendTextRequest request
                return match result.StatusCode with
                       | Some 201 -> match result.Content |> deserializeJsonWith<Response> [ InsertResultConverter() :> JsonConverter ] with
                                     | Ok o -> Created o
                                     | Error e -> JsonDeserializationError <| RequestResult.createForJson(e, result.StatusCode, result.Headers)
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
    
