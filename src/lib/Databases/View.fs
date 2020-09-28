namespace b0wter.CouchDb.Lib.Databases

//
// Queries: /{db}/_find [POST]
//

open b0wter.CouchDb.Lib
open b0wter.CouchDb.Lib.Core
open b0wter.FSharp
open Newtonsoft.Json
open Newtonsoft.Json.Linq

module View =

    /// This type is only required for internal use and makes the
    /// deserialization code cleaner. It cannot be made private because
    /// Newtonsoft.Json does not work with private types.
    /// A row contains a single response from a view.
    /// A view will always return a list of rows.
    type Row<'key, 'value> = {
        id: string
        key: 'key
        value: 'value
    }

    /// Response for a single successful query.
    type SingleResponse<'key, 'value> = {
        offset: int
        rows: Row<'key, 'value> list
        [<JsonProperty("total_rows")>]
        totalRows: int
        // The `update_seq` property is missing
        // because it is dynamic.
    }

    /// This type is only required for internal use and makes the
    /// deserialization code cleaner. It cannot be made private because
    /// Newtonsoft.Json does not work with private types.
    type MultiQueryResponse<'key, 'value> = {
        results: SingleResponse<'key, 'value> list
    }

    type Response<'key, 'value>
        = Single of SingleResponse<'key, 'value>
        | Multi of SingleResponse<'key, 'value> list

    type Result<'key, 'value>
        = Success of Response<'key, 'value>
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

    /// Additional settings for a single view query.
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

    /// Instance of `SingleQueryParameters` with every property
    /// set to a default value.
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

    /// Parameter to execute multiple queries for the given view.
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
    let asResult (r: Result<_, _>) =
        match r with
        | Success x -> Ok x
        | BadRequest e | Unauthorized e | JsonDeserializationError e | NotFound e | Unknown e ->
            Error <| ErrorRequestResult.fromRequestResultAndCase(e, r)

    /// Queries the server and does some basic parsing.
    /// The documents are not deserialized to objects but kept in a JObject list.
    /// This allows the user to perform dynamic operations.
    /// The `JOject` has a single property named `docs` that contains a list of `JObjects`.
    let private jObjectsQuery<'key> (props: DbProperties.T) (dbName: string) (designDoc: string) (view: string) (queryParameters: QueryParameters) : Async<Core.Result<Response<'key, JObject> * RequestResult.StatusCode * RequestResult.Headers, RequestResult.T>> =
        async {
            let isSingleQuery = match queryParameters with | Single _ -> true | Multi _ -> false
            let url = if isSingleQuery then
                        sprintf "%s/_design/%s/_view/%s" dbName designDoc view
                      else
                        sprintf "%s/_design/%s/_view/%s/queries" dbName designDoc view
            // The match makes sure that we dont need a custom json converter that serializes the content in a "non-union-type-way".
            let request = match queryParameters with
                          | Single s -> createCustomJsonPost props url [] s []
                          | Multi m -> createCustomJsonPost props url [] m []
            
            let! result = sendRequest request
            
            if result.statusCode.IsSome && result.statusCode.Value = 200 then
                let results = if isSingleQuery then result.content |> deserializeJson<SingleResponse<'key, JObject>> |> Result.map List.singleton
                              else result.content |> deserializeJson<MultiQueryResponse<'key, JObject>> |> Result.map (fun x -> x.results)
                
                return match results with
                       | Ok singles when isSingleQuery ->
                            Ok (Response.Single (singles |> List.exactlyOne), result.statusCode, result.headers)
                       | Ok singles ->
                            Ok (Response.Multi singles, result.statusCode, result.headers)
                       | Error e ->
                            Error (RequestResult.createForJson(e, result.statusCode, result.headers))

            else
                return Error <| RequestResult.createWithHeaders(result.statusCode, result.content, result.headers)
        }

    /// Deserializes a `JObject` as the given `value`.
    let private mapSingleResponse<'key, 'value> (response: SingleResponse<'key, JObject>) : FSharp.Core.Result<SingleResponse<'key, 'value>, string> =
        let rec step (acc: Row<'key, 'value> list) (remaining: Row<'key, JObject> list) : FSharp.Core.Result<SingleResponse<'key, 'value>, string> =
            match remaining with
            | [] -> 
                let r = Ok { SingleResponse.offset = response.offset; SingleResponse.totalRows = response.totalRows; SingleResponse.rows = (acc |> List.rev) }
                r
            | head :: tail -> match head.value |> Json.JObject.toObject<'value> with
                              | Ok converted -> step ({ id = head.id; key = head.key; value = converted } :: acc) tail
                              | Error e -> Core.Result<SingleResponse<'key, 'value>, string>.Error e
        step [] response.rows

    /// Queries the given view of the design document and converts the emitted keys to `'key` and the values of the rows to `'value`.
    /// Allows the definition of query parameters. These will be sent in the POST body (not as query parameters in a GET request).
    let queryWith<'key, 'value> (props: DbProperties.T) (dbName: string) (designDoc: string) (view: string) (queryParameters: QueryParameters) : Async<Result<'key, 'value>> =
        async {
            match! jObjectsQuery props dbName designDoc view queryParameters with
            | Ok (o, statusCode, headers) -> 
                match o with
                | Response.Single s -> 
                    match s |> mapSingleResponse with
                    | Ok mapped -> return Success (Response.Single mapped)
                    | Error e ->
                        let error = JsonDeserializationError.create(s.rows.ToString(), sprintf "Error while converting `JObjects` to the actual objects: %s" e)
                        return JsonDeserializationError (RequestResult.createForJson(error, statusCode, headers))
                | Response.Multi m ->
                    match m |> List.map mapSingleResponse |> Utilities.switchListResult with
                    | Ok mapped -> return Success (Response.Multi mapped)
                    | Error e ->
                        let error = JsonDeserializationError.create(sprintf "The serialization to provide this output was performed without custom converters!%s%s" System.Environment.NewLine (JsonConvert.SerializeObject(m)), sprintf "Error while converting `JObjects` to the actual objects: %s" e)
                        return JsonDeserializationError (RequestResult.createForJson(error, statusCode, headers))
            | Error e -> return (mapError e)
        }

    /// Queries the given view of the design document and converts the emitted keys to `'key` and the values of the rows to `'value`.
    /// Does not allow the definition of query parameters. Use `queryWith` instead.
    let query<'key, 'value> (props: DbProperties.T) (dbName: string) (designDoc: string) (view: string) =
        queryWith<'key, 'value> props dbName designDoc view (Single EmptyQueryParameters)
        
    /// Queries the given view of the design document and converts only the emitted keys to `'key`. The values are returned as `JObject`s.
    /// Allows the definition of query parameters. These will be sent in the POST body (not as query parameters in a GET request).
    let queryJObjectsWith<'key> (props: DbProperties.T) (dbName: string) (designDoc: string) (view: string) (queryParameters: QueryParameters) : Async<Result<'key, JObject>> =
        async {
            let! result = jObjectsQuery<'key> props dbName designDoc view queryParameters
            return match result with
                    | Ok (r, _, _) -> Success r
                    | Error e -> mapError e
        }

    /// Queries the given view of the design document and converts only the emitted keys to `'key`. The values are returned as `JObject`s.
    /// Does not allow the definition of query parameters. Use `queryWith` instead.
    let queryJObjects<'key> (props: DbProperties.T) (dbName: string) (designDoc: string) (view: string) : Async<Result<'key, JObject>> =
        queryJObjectsWith<'key> props dbName designDoc view (QueryParameters.Single EmptyQueryParameters)

    /// Runs `queryObjects` followed by `asResult`.
    let queryJObjectsWithAsResult props dbName designDoc view queryParameters =
        queryJObjectsWith props dbName designDoc view queryParameters |> Async.map asResult

    /// Runs `queryObjectsWith` followed by `asResult`.
    let queryJObjectsAsResult props dbName designDoc view =
        queryJObjectsWithAsResult props dbName designDoc view (QueryParameters.Single EmptyQueryParameters)
        
    /// Runs `queryWith` followed by `asResult`.
    let queryWithAsResult<'key, 'value> props dbName designDoc view queryParameters = queryWith<'key, 'value> props dbName designDoc view queryParameters |> Async.map asResult

    /// Runs `query` followed by `asResult`.
    let queryAsResult<'key, 'value> props dbName designDoc view = query<'key, 'value> props dbName designDoc view |> Async.map asResult

    /// Returns all rows of a response in a single list.
    /// If the response is a `Response.Multi` then the items of all lists will be collected.
    let responseAsRows (response: Response<_, 'value>) : Row<'a, 'value> list =
        match response with
        | Response.Single s -> s.rows
        | Response.Multi m -> m |> List.collect (fun r -> r.rows)

    /// Returns the response as a list of `SingleResponse`.
    /// Will return a list with a single element for a single response query.
    let responseAsSingleResponses (response: Response<_, _>) : SingleResponse<_, _> list =
        match response with
        | Response.Single s -> [ s ]
        | Response.Multi m -> m