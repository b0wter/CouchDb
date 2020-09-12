namespace b0wter.CouchDb.Lib.Databases

//
// Queries: /{db}/_find [POST]
//

open b0wter.CouchDb.Lib
open b0wter.CouchDb.Lib.Core
open b0wter.FSharp
open Newtonsoft.Json
open b0wter.CouchDb.Lib

module View =

    type SingleResponse<'a> = {
        offset: int
        rows: 'a list
        totalRows: int
        // The `update_seq` property is missing
        // because it is dynamic.
    }

    type Response<'a>
        = Single of SingleResponse<'a>
        | Multi of SingleResponse<'a> list

    type MetaData = {
        offset: int
        [<JsonProperty("total_rows")>]
        totalRows: int
    }

    type Result<'a>
        = Success of Response<'a>
        /// Invalid request
        | BadRequest of RequestResult.T
        /// Read permission required
        | Unauthorized of RequestResult.T
        /// The database with the given name could not be found.
        | NotFound of RequestResult.T
        /// If the local deserialization of the servers response failed.
        | JsonDeserializationError of RequestResult.T
        /// If the response from the server could not be interpreted.
        | Unknown of RequestResult.T

    type SingleQueryParameters = {
        conflicts: bool option
        descending: bool option
        [<JsonProperty("end_key")>]
        endKey: string option
        [<JsonProperty("end_key_doc_id")>]
        endKeyDocId: string option
        group: bool option
        [<JsonProperty("group_level")>]
        groupLevel: int option
        [<JsonProperty("include_docs")>]
        includeDocs: bool option
        attachments: bool option
        [<JsonProperty("att_encoding_info")>]
        attachmentEncodingInfo: bool option
        [<JsonProperty("inclusive_end")>]
        inclusiveEnd: bool option
        keys: string list option
        key: string option
        limit: int option
        reduce: bool option
        skip: int option
        sorted: bool option
        stable: bool option
        stale: string option
        [<JsonProperty("startkey")>]
        startKey: string option
        [<JsonProperty("start_key_doc_id")>]
        startKeyDocId: string option
        update: string option
        [<JsonProperty("update_seq")>]
        updateSeq: bool option
    }

    let EmptyQueryParameters = {
        conflicts = None
        descending = None
        endKey = None
        endKeyDocId = None
        group = None
        groupLevel = None
        includeDocs = None
        attachments = None
        attachmentEncodingInfo = None
        inclusiveEnd = None
        keys = None
        key = None
        limit = None
        reduce = None
        skip = None
        sorted = None
        stable = None
        stale = None
        startKey = None
        startKeyDocId = None
        update = None
        updateSeq = None
    }

    type MultiQueryParameters = {
        queries: SingleQueryParameters list
    }

    type QueryParameters
        = Single of SingleQueryParameters
        | Multi of MultiQueryParameters

    /// Turns a `RequestResult.T` into an actual `Result<'a>`.
    /// It will never return `Success` because that takes a `Response<'a>` as parameter.
    let private mapError (r: RequestResult.T) =
        match r.statusCode with
        | Some 400 -> BadRequest r
        | Some 401 -> Unauthorized r
        | Some 404 -> NotFound r
        | Some 200 -> JsonDeserializationError r
        | _ ->
            Unknown r


    /// Returns the result from the query as a generic `FSharp.Core.Result`.
    let asResult<'a> (r: Result<'a>) =
        match r with
        | Success x -> Ok x
        | BadRequest e | Unauthorized e | JsonDeserializationError e | NotFound e | Unknown e ->
            Error <| ErrorRequestResult.fromRequestResultAndCase(e, r)

    /// Queries the server and does some basic parsing.
    /// The documents are not deserialized to objects but kept in a JObject list.
    /// This allows the user to perform dynamic operations.
    /// The `JOject` has a single property named `docs` that contains a list of `JObjects`.
    let private jObjectsQuery (props: DbProperties.T) (dbName: string) (designDoc: string) (view: string) (queryParameters: QueryParameters) =
        async {
            let url = sprintf "%s/_design/%s/_view/%s" dbName designDoc view
            // The match makes sure that we dont need a custom json converter that serializes the content in a "non-union-type-way".
            let request = match queryParameters with
                          | Single s -> createCustomJsonPost props url [] s []
                          | Multi m -> createCustomJsonPost props url [] m []
            let! result = sendRequest request
            if result.statusCode.IsSome && result.statusCode.Value = 200 then
                let deserializationKey = match queryParameters with | Single _ -> "rows" | Multi _ -> "results"
                let objects = result.content |> Json.JObject.asJObject |> Result.bind (Json.JObject.getProperty deserializationKey) |> Result.bind Json.JObject.getJArray |> Result.bind Json.JObject.jArrayAsJObjects
                //TODO: The metadata is not extracted properly for multiple queries.
                let metaData = objects |> Result.bind (fun r -> deserializeJson<MetaData> result.content |> Result.mapError JsonDeserializationError.asString)
                return match objects, metaData with
                       | Ok a, Ok m -> 
                           match queryParameters with
                           | Single _ -> 
                                Ok (Response.Single { SingleResponse.rows = a; SingleResponse.offset = m.offset; SingleResponse.totalRows = m.totalRows }, result.statusCode, result.headers)
                           | Multi _ ->
                                failwith "rekt"
                       | Error e, _ -> 
                           let jsonError = JsonDeserializationError.create(result.content, sprintf "Error occured while deserializing the `rows/results property and transforming its contents into `JObject`s: %s" e)
                           let requestResult = RequestResult.createForJson(jsonError, result.statusCode, result.headers)
                           Error <| requestResult
                       | _, Error e ->
                           let jsonError = JsonDeserializationError.create(result.content, sprintf "Error occured while deserializing the metadata and transforming its contents into a `JObject`: %s" e)
                           let requestResult = RequestResult.createForJson(jsonError, result.statusCode, result.headers)
                           Error <| requestResult
                           (*
                           let requestResult = RequestResult.createForJson({e with reason = sprintf "Error occured while deserializing the meta data: %s" e.reason }, result.statusCode, result.headers)
                           Error <| requestResult
                           *)
            else
                return Error <| RequestResult.createWithHeaders(result.statusCode, result.content, result.headers)
        }

    /// Is build on top of `jObjectsQuery` and uses `Json.JObject.toObjects` to deserialize the `JObject list` into a list of actual objects.
    let private queryWith<'a> (props: DbProperties.T) (dbName: string) (designDoc: string) (view: string) (queryParameters: QueryParameters) : Async<Result<'a>> =
        async {
            match! jObjectsQuery props dbName designDoc view queryParameters with
            | Ok (o, statusCode, headers) -> 
                match o with
                | Response.Single s -> 
                    match s.rows |> Json.JObject.toObjects<'a> with
                    | Ok docs -> 
                        return Success (Response.Single { SingleResponse.rows = docs; SingleResponse.offset = s.offset; SingleResponse.totalRows = s.totalRows })
                    | Error e -> 
                        let error = JsonDeserializationError.create(s.rows.ToString(), sprintf "Error while converting `JObjects` to the actual objects: %s" e)
                        return JsonDeserializationError (RequestResult.createForJson(error, statusCode, headers))
                | Response.Multi m ->
                    return failwith "multi"
                (*
                match o.rows |> Json.JObject.toObjects<'a> with
                | Ok docs -> 
                    return Success ({ Response.rows = docs; Response.offset = o.offset; Response.totalRows = o.totalRows })
                | Error e -> 
                    let error = JsonDeserializationError.create(o.rows.ToString(), sprintf "Error while converting `JObjects` to the actual objects: %s" e)
                    return JsonDeserializationError (RequestResult.createForJson(error, statusCode, headers))
                    *)
            | Error e -> return (mapError e)
        }

    /// Queries the database using a custom-built mango expression. 
    /// If you want to print the serialized operator use `queryWithOutput` instead.
    let query<'a> (props: DbProperties.T) (dbName: string) (designDoc: string) (view: string)  (queryParameters: QueryParameters)=
        queryWith<'a> props dbName designDoc view queryParameters


    /// Is build on top of `jObjectsQuery` and maps the result into a `Database.Find.Result<JObject>`.
    let private queryJObjectsWith (props: DbProperties.T) (dbName: string) (designDoc: string) (view: string)  (queryParameters: QueryParameters): Async<Result<Linq.JObject>> =
        async {
            let! result = jObjectsQuery props dbName designDoc view queryParameters
            return match result with
                    | Ok (r, _, _) -> Success r
                    | Error e -> mapError e
        }


    /// Runs `queryObjects` followed by `asResult`.
    let queryObjectsAsResult props dbName designDoc view queryParameters =
        queryJObjectsWith props dbName designDoc view queryParameters |> Async.map asResult<Linq.JObject>
        

    /// Runs `query` followed by `asResult`.
    let queryAsResult<'a> props dbName designDoc view queryParameters = query<'a> props dbName designDoc view queryParameters |> Async.map asResult<'a>

    (*
    /// Retrieves the first element of a successful query or an error message.
    /// Useful if you know that your query will return a single element.
    /// Also returns an error if the query is successful but did not return any documents.
    let getFirst (r: Result<'a>) : Result<'a, string> =
        r
        |> asResult
        |> Result.mapBoth (fun ok -> ok.rows |> List.tryHead)  (fun error -> sprintf "[%s] %s" error.case error.content)
        |> function
           | Ok (Some o) -> Ok o
           | Ok None -> Error "The query was successful but did not return any documents."
           | Error e -> Error e
    *)
