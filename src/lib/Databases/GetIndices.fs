namespace b0wter.CouchDb.Lib.Databases

//
// Queries: /{db}/_index [POST]
//

open b0wter.CouchDb.Lib
open b0wter.CouchDb.Lib.Core
open b0wter.FSharp
open Newtonsoft.Json.Linq
open Newtonsoft.Json
open System.Collections.Generic

module GetIndices =

    type SortOrder
        = Ascending
        | Descending

    type Field = {
        Name: string
        Sorting: SortOrder
    }

    type Definition = {
        Fields: Field list
        [<JsonProperty("partial_filter_selector")>]
        Selector: JObject option
    }

    type Index = {
        /// Name of the index.
        Name: string
        /// ID of the design document the index belongs to. 
        /// This ID can be used to retrieve the design document containing the index, 
        /// by making a `GET` request to `/db/ddoc`, where ddoc is the value of this field.
        DDoc: string
        /// Type of the index. Currently “json” is the only supported type.
        /// Although there are third-party plugin options.
        Type: string
        Partitioned: bool
        [<JsonProperty("def")>]
        Definition: Definition
    }

    type FieldConverter() =
        inherit JsonConverter()

        override this.CanRead = true

        override this.CanConvert(t) =
            typeof<Field>.IsAssignableFrom(t)

        override this.ReadJson(reader, objectType, existingValue, serializer) =
            let json = JObject.Load(reader)
            let prop = (json.First :?> JProperty)
            let name = prop.Name
            let value = prop.Value.ToString() |> function "asc" -> SortOrder.Ascending | "desc" -> SortOrder.Descending | s -> failwith (sprintf "Unknown sort order: %s" s)
            { Name = name; Sorting = value } :> obj

        override this.WriteJson(writer, value, _) =
            failwith "The 'FieldConverter' does not currently support writing json."

    type Response = {
        [<JsonProperty("total_rows")>]
        TotalRows: int
        Indexes: Index list
    }

    type Result
        = Success of Response
        | NotFound of RequestResult.T
        | BadRequest of RequestResult.T
        | Unauthorized of RequestResult.T
        | InternalServerError of RequestResult.T
        | JsonDeserializationError of RequestResult.T
        | DbNameMissing of RequestResult.T
        | Unknown of RequestResult.T

    /// When you make a `GET` request to `/db/_index`, you get a list of all indexes in the database. 
    /// In addition to the information available through this API, 
    /// indexes are also stored in design documents <index-functions>. 
    /// Design documents are regular documents that have an ID starting with `_design/`. 
    /// Design documents can be retrieved and modified in the same way as any other document, 
    /// although this is not necessary when using Mango.
    let query (props: DbProperties.T) (dbName: string) : Async<Result> =
        async {
            if System.String.IsNullOrWhiteSpace(dbName) then return DbNameMissing <| RequestResult.create(None, "No query was sent to the server. You supplied an empty db name.") else
            let url = sprintf "%s/_index" dbName
            let request = createGet props url []
            let! result = sendRequest request
            return match result.StatusCode with
                    | Some 200 -> match deserializeJsonWith [ FieldConverter() ] result.Content with
                                    | Ok r -> Success r
                                    | Error e -> JsonDeserializationError <| RequestResult.createForJson(e, result.StatusCode, result.Headers)
                    | Some 400 -> BadRequest result
                    | Some 401 -> Unauthorized result
                    | Some 404 -> NotFound result
                    | Some 500 -> InternalServerError result
                    | _ -> Unknown result
        }
    
    /// Returns the result from the query as a generic `FSharp.Core.Result`.
    let asResult (r: Result) =
        match r with
        | Success response -> Ok response
        | NotFound e | Unauthorized e | BadRequest e | InternalServerError e | JsonDeserializationError e | DbNameMissing e | Unknown e -> Error e

    /// When you make a `GET` request to `/db/_index`, you get a list of all indexes in the database. 
    /// In addition to the information available through this API, 
    /// indexes are also stored in design documents <index-functions>. 
    /// Design documents are regular documents that have an ID starting with `_design/`. 
    /// Design documents can be retrieved and modified in the same way as any other document, 
    /// although this is not necessary when using Mango.
    let queryAsResult props name = query props name |> Async.map asResult