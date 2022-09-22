namespace b0wter.CouchDb.Lib.Partitions

//
// Queries: /{db}/_partition/{partition}/_all_docs [GET]
//

open Newtonsoft.Json
open Newtonsoft.Json.Linq
open b0wter.CouchDb.Lib
open Core

module AllDocs =
    /// This type is only required for internal use and makes the
    /// deserialization code cleaner. It cannot be made private because
    /// Newtonsoft.Json does not work with private types.
    /// A row contains a single response from a view.
    /// A view will always return a list of rows.
    type Row<'key, 'value> = {
        Id: string
        Key: 'key
        Value: {| Rev: string option |}
        Doc: 'value option
    }

    /// Response for a single successful query.
    type Response<'key, 'value> = {
        Offset: int
        Rows: Row<'key, 'value> list
        [<JsonProperty("total_rows")>]
        TotalRows: int
    }

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
    type QueryParameters = {
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
    let private jObjectsQuery<'key> (props: DbProperties.DbProperties) (dbName: string) (partition: string) (queryParameters: QueryParameters) : Async<Core.Result<Response<'key, JObject> * RequestResult.StatusCode * RequestResult.Headers, RequestResult.StringRequestResult>> =
        async {
            let url = sprintf "/%s/_partition/%s/_all_docs" dbName partition
            // The match makes sure that we dont need a custom json converter that serializes the content in a "non-union-type-way".
            let request = createCustomJsonPost props url [] queryParameters []
            
            let! result = sendTextRequest request
            
            if result.StatusCode.IsSome && result.StatusCode.Value = 200 then
                return
                    result.Content
                    |> deserializeJson<Response<'key, JObject>>
                    |> Result.map (fun r -> r, result.StatusCode, result.Headers)
                    |> Result.mapError (fun error -> RequestResult.createForJson(error, result.StatusCode, result.Headers))
            else
                return Error <| RequestResult.createTextWithHeaders(result.StatusCode, result.Content, result.Headers)
        }

    /// Deserializes a `JObject` as the given `value`.
    let private mapSingleResponse<'key, 'value> (response: Response<'key, JObject>) : FSharp.Core.Result<Response<'key, 'value>, string> =
        let rec step (acc: Row<'key, 'value> list) (remaining: Row<'key, JObject> list) : FSharp.Core.Result<Response<'key, 'value>, string> =
            match remaining with
            | [] -> 
                Ok { Response.Offset = response.Offset; Response.TotalRows = response.TotalRows; Response.Rows = (acc |> List.rev) }
            | head :: tail -> match head.Doc |> Option.map (Json.JObject.toObject<'value> []) with
                              | Some (Ok converted) -> step ({ Id = head.Id; Key = head.Key; Value = head.Value; Doc = Some converted } :: acc) tail
                              | Some (Error e) -> Core.Result<Response<'key, 'value>, string>.Error e
                              | None -> step ({ Id = head.Id; Key = head.Key; Value = head.Value; Doc = None } :: acc) tail
        step [] response.Rows

    /// Queries the given view of the design document and converts the emitted keys to `'key` and the values of the rows to `'value`.
    /// Allows the definition of query parameters. These will be sent in the POST body (not as query parameters in a GET request).
    let queryWith<'key, 'value> (props: DbProperties.DbProperties) (dbName: string) (partition: string) (queryParameters: QueryParameters) : Async<Result<'key, 'value>> =
        async {
            match! jObjectsQuery<'key> props dbName partition queryParameters with
            | Ok (o, statusCode, headers) -> 
                match o |> mapSingleResponse with
                | Ok mapped -> return Success mapped
                | Error e ->
                    let error = JsonDeserializationError.create(o.Rows.ToString(), sprintf "Error while converting `JObjects` to the actual objects: %s" e)
                    return JsonDeserializationError (RequestResult.createForJson(error, statusCode, headers))
            | Error e -> return (mapError e)
        }

    /// Queries the given view of the design document and converts the emitted keys to `'key` and the values of the rows to `'value`.
    /// Does not allow the definition of query parameters. Use `queryWith` instead.
    let query<'key, 'value> (props: DbProperties.DbProperties) (dbName: string) (partition: string) =
        queryWith<'key, 'value> props dbName partition EmptyQueryParameters
        
    /// Queries the given view of the design document and converts only the emitted keys to `'key`. The values are returned as `JObject`s.
    /// Allows the definition of query parameters. These will be sent in the POST body (not as query parameters in a GET request).
    let queryJObjectsWith<'key> (props: DbProperties.DbProperties) (dbName: string) (partition: string) (queryParameters: QueryParameters) : Async<Result<'key, JObject>> =
        async {
            let! result = jObjectsQuery<'key> props dbName partition queryParameters
            return match result with
                    | Ok (r, _, _) -> Success r
                    | Error e -> mapError e
        }

    /// Queries the given view of the design document and converts only the emitted keys to `'key`. The values are returned as `JObject`s.
    /// Does not allow the definition of query parameters. Use `queryWith` instead.
    let queryJObjects<'key> (props: DbProperties.DbProperties) (dbName: string) (partition: string) : Async<Result<'key, JObject>> =
        queryJObjectsWith<'key> props dbName partition EmptyQueryParameters

    /// Runs `queryObjects` followed by `asResult`.
    let queryJObjectsWithAsResult<'key> props dbName partition queryParameters =
        queryJObjectsWith<'key> props dbName partition queryParameters |> Utilities.Async.map asResult

    /// Runs `queryObjectsWith` followed by `asResult`.
    let queryJObjectsAsResult<'key> props dbName partition =
        queryJObjectsWithAsResult<'key> props dbName partition EmptyQueryParameters
        
    /// Runs `queryWith` followed by `asResult`.
    let queryWithAsResult<'key, 'value> props dbName partition queryParameters = queryWith<'key, 'value> props dbName partition queryParameters |> Utilities.Async.map asResult

    /// Runs `query` followed by `asResult`.
    let queryAsResult<'key, 'value> props dbName partition = query<'key, 'value> props dbName partition |> Utilities.Async.map asResult

    /// Returns all rows of a response in a single list.
    /// If the response is a `Response.Multi` then the items of all lists will be collected.
    let responseAsRows (response: Response<_, 'value>) : Row<'a, 'value> list = response.Rows
