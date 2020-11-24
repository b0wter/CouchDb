namespace b0wter.CouchDb.Lib.Indexes

//
// Queries: /{db}/_index [POST]
//

open b0wter.CouchDb.Lib
open b0wter.CouchDb.Lib.Core
open b0wter.FSharp
open Newtonsoft.Json.Linq
open Newtonsoft.Json

module Create =

    type Index = {
        /// Field names following the sort syntax. Nested fields are also allowed, e.g. “person.name”.
        Fields: string list
        /// A selector to apply to documents at indexing time, creating a partial index
        [<JsonPropertyAttribute("partial_filter_selector")>]
        Selector: Mango.Expression option
    }

    let createFieldsIndex (fields: string list) = { Fields = fields; Selector = None}

    let createSelectorIndex (selector: Mango.Expression) = { Fields = []; Selector = Some selector }

    let createFieldsAndSelectorIndex (fields: string list) (selector: Mango.Expression) = { Fields = fields; Selector = Some selector }

    type QueryParameters = {
        /// Object describing the index (or indices) to create.
        Index: Index
        /// Name of the design document in which the index will be created. 
        /// By default, each index will be created in its own design document. 
        /// Indexes can be grouped into design documents for efficiency. 
        /// However, a change to one index in a design document will invalidate 
        /// all other indexes in the same document (similar to views).
        [<JsonProperty("ddoc")>]
        DDoc: string option
        /// Name of the index. If no name is provided, 
        /// a name will be generated automatically.
        Name: string option
        [<JsonProperty("partial_filter_selector")>]
        PartialFilterSelector: Mango.Expression option
        /// Determines whether a JSON index is partitioned or global. 
        /// The default value of `partitioned` is the partitioned property of the database. 
        /// To create a global index on a partitioned database, 
        /// specify `false` for the "partitioned" field. 
        /// If you specify `true` for the `"partitioned"` field on an unpartitioned database, 
        /// an error occurs.
        Partitioned: bool option
    }

    /// Empty query parameters. Use this with the `with`-keyword to quickly
    /// create custom query parameters.
    let EmptyQueryParameters = {
        Index = { Fields = []; Selector = None }
        DDoc = None
        Name = None
        PartialFilterSelector = None
        Partitioned = None
    }

    type Response = {
        /// Flag to show whether the index was created or one already exists. Can be “created” or “exists”.
        Result: string
        /// Id of the design document the index was created in.
        Id: string
        /// Name of the index created.
        Name: string
    }

    type Result
        = Success of Response
        /// Invalid request
        | BadRequest of RequestResult.TString
        /// Admin permission required
        | NotAuthorized of RequestResult.TString
        /// Execution error
        | InternalServerError of RequestResult.TString
        /// The given database does not exist.
        | NotFound of RequestResult.TString
        /// A client side error occured while trying to deserialize an incoming response.
        | JsonDeserializationError of RequestResult.TString
        /// You tried to make a query without a database name or with an empty database name.
        /// This error occurs locally. No requests have been sent to the server.
        | DbNameMissing of RequestResult.TString
        /// Catch-all for unhandled error cases.
        | Unknown of RequestResult.TString

    /// Create a new index on a database.
    /// Mango is a declarative JSON querying language for CouchDB databases. 
    /// Mango wraps several index types, starting with the Primary Index out-of-the-box. 
    /// Mango indexes, with index type json, are built using MapReduce Views.
    /// 
    /// **IMPORTANT**: only `json` type indices are supported.
    let query (props: DbProperties.T) (name: string) (queryParameters: QueryParameters) : Async<Result> =
        async {
            if System.String.IsNullOrWhiteSpace(name) then return DbNameMissing <| RequestResult.createText(None, "No query was sent to the server. You supplied an empty db name.") else
            let url = sprintf "%s/_index" name
            let request = createCustomJsonPost props url [ MangoConverters.OperatorJsonConverter(false) ] queryParameters []
            let! result = sendTextRequest request
            return match result.StatusCode with
                    | Some 200 -> match deserializeJson result.Content with
                                    | Ok r -> 
                                        Success r
                                    | Error e -> JsonDeserializationError <| RequestResult.createForJson(e, result.StatusCode, result.Headers)
                    | Some 400 -> BadRequest result
                    | Some 404 -> NotFound result
                    | Some 401 -> NotAuthorized result
                    | Some 500 -> InternalServerError result
                    | _ -> Unknown result
        }
    
    /// Returns the result from the query as a generic `FSharp.Core.Result`.
    let asResult (r: Result) =
        match r with
        | Success response -> Ok response
        | JsonDeserializationError e | DbNameMissing e | NotFound e | NotAuthorized e | InternalServerError e | BadRequest e | Unknown e -> Error e

    /// Runs query followed by asResult.
    let queryAsResult props name queryParameters = query props name queryParameters |> Async.map asResult
