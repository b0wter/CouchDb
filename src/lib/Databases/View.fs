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
        Id: string
        Key: 'key
        Value: 'value
    }

    /// Response for a single successful query.
    type SingleResponse<'key, 'value> = {
        Offset: int
        Rows: Row<'key, 'value> list
        [<JsonProperty("total_rows")>]
        TotalRows: int
        // The `update_seq` property is missing
        // because it is dynamic.
    }

    /// This type is only required for internal use and makes the
    /// deserialization code cleaner. It cannot be made private because
    /// Newtonsoft.Json does not work with private types.
    type MultiQueryResponse<'key, 'value> = {
        Results: SingleResponse<'key, 'value> list
    }

    type Response<'key, 'value>
        = Single of SingleResponse<'key, 'value>
        | Multi of SingleResponse<'key, 'value> list

    type Result<'key, 'value>
        = Success of Response<'key, 'value>
        /// Invalid request
        | BadRequest of RequestResult.StringRequestResult
        /// Read permission required
        | Unauthorized of RequestResult.StringRequestResult
        /// The database with the given name could not be found.
        | NotFound of RequestResult.StringRequestResult
        /// If the local deserialization of the servers response failed.
        | JsonDeserializationError of RequestResult.StringRequestResult
        /// If the response from the server could not be interpreted.
        | Unknown of RequestResult.StringRequestResult

    /// Additional settings for a single view query.
    type SingleQueryParameters = {
        Conflicts: bool option
        Descending: bool option
        [<JsonProperty("end_key")>]
        EndKey: string option
        [<JsonProperty("end_key_doc_id")>]
        EndKeyDocId: string option
        Group: bool option
        [<JsonProperty("group_level")>]
        GroupLevel: int option
        [<JsonProperty("include_docs")>]
        IncludeDocs: bool option
        Attachments: bool option
        [<JsonProperty("att_encoding_info")>]
        AttachmentEncodingInfo: bool option
        [<JsonProperty("inclusive_end")>]
        InclusiveEnd: bool option
        Keys: string list option
        Key: string option
        Limit: int option
        Reduce: bool option
        Skip: int option
        Sorted: bool option
        Stable: bool option
        Stale: string option
        [<JsonProperty("startkey")>]
        StartKey: string option
        [<JsonProperty("start_key_doc_id")>]
        StartKeyDocId: string option
        Update: string option
        [<JsonProperty("update_seq")>]
        UpdateSeq: bool option
    }

    /// Instance of `SingleQueryParameters` with every property
    /// set to a default value.
    let EmptyQueryParameters = {
        Conflicts = None
        Descending = None
        EndKey = None
        EndKeyDocId = None
        Group = None
        GroupLevel = None
        IncludeDocs = None
        Attachments = None
        AttachmentEncodingInfo = None
        InclusiveEnd = None
        Keys = None
        Key = None
        Limit = None
        Reduce = None
        Skip = None
        Sorted = None
        Stable = None
        Stale = None
        StartKey = None
        StartKeyDocId = None
        Update = None
        UpdateSeq = None
    }

    /// Parameter to execute multiple queries for the given view.
    type MultiQueryParameters = {
        Queries: SingleQueryParameters list
    }

    type QueryParameters
        = Single of SingleQueryParameters
        | Multi of MultiQueryParameters

    /// Turns a `RequestResult.TString` into an actual `Result<'a>`.
    /// It will never return `Success` because that takes a `Response<'a>` as parameter.
    let private mapError (r: RequestResult.StringRequestResult) =
        match r.StatusCode with
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
    let private jObjectsQuery<'key> (props: DbProperties.DbProperties) (dbName: string) (designDoc: string) (view: string) (queryParameters: QueryParameters) : Async<Core.Result<Response<'key, JObject> * RequestResult.StatusCode * RequestResult.Headers, RequestResult.StringRequestResult>> =
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
            
            let! result = sendTextRequest request
            
            if result.StatusCode.IsSome && result.StatusCode.Value = 200 then
                let results = if isSingleQuery then result.Content |> deserializeJson<SingleResponse<'key, JObject>> |> Result.map List.singleton
                              else result.Content |> deserializeJson<MultiQueryResponse<'key, JObject>> |> Result.map (fun x -> x.Results)
                
                return match results with
                       | Ok singles when isSingleQuery ->
                            Ok (Response.Single (singles |> List.exactlyOne), result.StatusCode, result.Headers)
                       | Ok singles ->
                            Ok (Response.Multi singles, result.StatusCode, result.Headers)
                       | Error e ->
                            Error (RequestResult.createForJson(e, result.StatusCode, result.Headers))

            else
                return Error <| RequestResult.createTextWithHeaders(result.StatusCode, result.Content, result.Headers)
        }

    /// Deserializes a `JObject` as the given `value`.
    let private mapSingleResponse<'key, 'value> (response: SingleResponse<'key, JObject>) : FSharp.Core.Result<SingleResponse<'key, 'value>, string> =
        let rec step (acc: Row<'key, 'value> list) (remaining: Row<'key, JObject> list) : FSharp.Core.Result<SingleResponse<'key, 'value>, string> =
            match remaining with
            | [] -> 
                let r = Ok { SingleResponse.Offset = response.Offset; SingleResponse.TotalRows = response.TotalRows; SingleResponse.Rows = (acc |> List.rev) }
                r
            | head :: tail -> match head.Value |> Json.JObject.toObject<'value> with
                              | Ok converted -> step ({ Id = head.Id; Key = head.Key; Value = converted } :: acc) tail
                              | Error e -> Core.Result<SingleResponse<'key, 'value>, string>.Error e
        step [] response.Rows

    /// Queries the given view of the design document and converts the emitted keys to `'key` and the values of the rows to `'value`.
    /// Allows the definition of query parameters. These will be sent in the POST body (not as query parameters in a GET request).
    let queryWith<'key, 'value> (props: DbProperties.DbProperties) (dbName: string) (designDoc: string) (view: string) (queryParameters: QueryParameters) : Async<Result<'key, 'value>> =
        async {
            match! jObjectsQuery props dbName designDoc view queryParameters with
            | Ok (o, statusCode, headers) -> 
                match o with
                | Response.Single s -> 
                    match s |> mapSingleResponse with
                    | Ok mapped -> return Success (Response.Single mapped)
                    | Error e ->
                        let error = JsonDeserializationError.create(s.Rows.ToString(), sprintf "Error while converting `JObjects` to the actual objects: %s" e)
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
    let query<'key, 'value> (props: DbProperties.DbProperties) (dbName: string) (designDoc: string) (view: string) =
        queryWith<'key, 'value> props dbName designDoc view (Single EmptyQueryParameters)
        
    /// Queries the given view of the design document and converts only the emitted keys to `'key`. The values are returned as `JObject`s.
    /// Allows the definition of query parameters. These will be sent in the POST body (not as query parameters in a GET request).
    let queryJObjectsWith<'key> (props: DbProperties.DbProperties) (dbName: string) (designDoc: string) (view: string) (queryParameters: QueryParameters) : Async<Result<'key, JObject>> =
        async {
            let! result = jObjectsQuery<'key> props dbName designDoc view queryParameters
            return match result with
                    | Ok (r, _, _) -> Success r
                    | Error e -> mapError e
        }

    /// Queries the given view of the design document and converts only the emitted keys to `'key`. The values are returned as `JObject`s.
    /// Does not allow the definition of query parameters. Use `queryWith` instead.
    let queryJObjects<'key> (props: DbProperties.DbProperties) (dbName: string) (designDoc: string) (view: string) : Async<Result<'key, JObject>> =
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
        | Response.Single s -> s.Rows
        | Response.Multi m -> m |> List.collect (fun r -> r.Rows)

    /// Returns the response as a list of `SingleResponse`.
    /// Will return a list with a single element for a single response query.
    let responseAsSingleResponses (response: Response<_, _>) : SingleResponse<_, _> list =
        match response with
        | Response.Single s -> [ s ]
        | Response.Multi m -> m