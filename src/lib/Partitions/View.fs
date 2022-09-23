namespace b0wter.CouchDb.Lib.Partitions

//
// Queries: /{db}/_partition/{partition}/_design/{design-doc}/_view/{view-name}
//

open b0wter.CouchDb.Lib
open Newtonsoft.Json.Linq

module View =
    /// This type is only required for internal use and makes the
    /// deserialization code cleaner. It cannot be made private because
    /// Newtonsoft.Json does not work with private types.
    /// A row contains a single response from a view.
    /// A view will always return a list of rows.
    type Row<'key, 'value> = Generic.View.Row<'key, 'value>
    
    /// Response for a single successful query.
    type SingleResponse<'key, 'value> = Generic.View.SingleResponse<'key, 'value>
    
    /// This type is only required for internal use and makes the
    /// deserialization code cleaner. It cannot be made private because
    /// Newtonsoft.Json does not work with private types.
    type MultiQueryResponse<'key, 'value> = Generic.View.MultiQueryResponse<'key, 'value>
    
    type Response<'key, 'value> = Generic.View.Response<'key, 'value>
    
    type Result<'key, 'value> = Generic.View.Result<'key, 'value>
    
    /// Additional settings for a single view query.
    type SingleQueryParameters = Generic.View.SingleQueryParameters
    
    /// Instance of `SingleQueryParameters` with every property
    /// set to a default value.
    let EmptyQueryParameters = Generic.View.EmptyQueryParameters
    
    /// Parameter to execute multiple queries for the given view.
    type MultiQueryParameters = Generic.View.MultiQueryParameters
    
    type QueryParameters = Generic.View.QueryParameters
    
    /// Returns the result from the query as a generic `FSharp.Core.Result`.
    let asResult = Generic.View.asResult
    
    /// Queries the given view of the design document and converts the emitted keys to `'key` and the values of the rows to `'value`.
    /// Allows the definition of query parameters. These will be sent in the POST body (not as query parameters in a GET request).
    let queryWith<'key, 'value> (props: DbProperties.DbProperties) (dbName: string) (partition: string) (designDoc: string) (view: string) (queryParameters: QueryParameters) : Async<Result<'key, 'value>> =
        Generic.View.queryWith<'key, 'value> props (Generic.View.FindIn.Partition {| dbName = dbName; partitionName = partition |}) designDoc view queryParameters
        
    /// Queries the given view of the design document and converts the emitted keys to `'key` and the values of the rows to `'value`.
    /// Does not allow the definition of query parameters. Use `queryWith` instead.
    let query<'key, 'value> (props: DbProperties.DbProperties) (dbName: string) (partition: string) (designDoc: string) (view: string) =
        Generic.View.query<'key, 'value> props (Generic.View.FindIn.Partition {| dbName = dbName; partitionName = partition |}) designDoc view
        
    /// Queries the given view of the design document and converts only the emitted keys to `'key`. The values are returned as `JObject`s.
    /// Allows the definition of query parameters. These will be sent in the POST body (not as query parameters in a GET request).
    let queryJObjectsWith<'key> (props: DbProperties.DbProperties) (dbName: string) (partition: string) (designDoc: string) (view: string) (queryParameters: QueryParameters) : Async<Result<'key, JObject>> =
        Generic.View.queryJObjectsWith<'key> props (Generic.View.FindIn.Partition {| dbName = dbName; partitionName = partition |}) designDoc view queryParameters
    
    /// Queries the given view of the design document and converts only the emitted keys to `'key`. The values are returned as `JObject`s.
    /// Does not allow the definition of query parameters. Use `queryWith` instead.
    let queryJObjects<'key> (props: DbProperties.DbProperties) (dbName: string) (partition: string) (designDoc: string) (view: string) : Async<Result<'key, JObject>> =
        Generic.View.queryJObjects<'key> props (Generic.View.FindIn.Partition {| dbName = dbName; partitionName = partition |}) designDoc view
        
    /// Runs `queryObjects` followed by `asResult`.
    let queryJObjectsWithAsResult<'key> props dbName partition designDoc view queryParameters =
        Generic.View.queryJObjectsWithAsResult<'key> props (Generic.View.FindIn.Partition {| dbName = dbName; partitionName = partition |}) designDoc view queryParameters
    
    /// Runs `queryObjectsWith` followed by `asResult`.
    let queryJObjectsAsResult<'key> props dbName partition designDoc view =
        Generic.View.queryJObjectsAsResult props (Generic.View.FindIn.Partition {| dbName = dbName; partitionName = partition |}) designDoc view
    
    /// Runs `queryWith` followed by `asResult`.
    let queryWithAsResult<'key, 'value> props dbName partition designDoc view queryParameters =
        Generic.View.queryWithAsResult<'key, 'value> props (Generic.View.FindIn.Partition {| dbName = dbName; partitionName = partition |}) designDoc view queryParameters

    /// Runs `query` followed by `asResult`.
    let queryAsResult<'key, 'value> props dbName partition designDoc view =
        Generic.View.queryAsResult<'key, 'value> props (Generic.View.FindIn.Partition {| dbName = dbName; partitionName = partition |}) designDoc view

    /// Returns all rows of a response in a single list.
    /// If the response is a `Response.Multi` then the items of all lists will be collected.
    let responseAsRows (response: Response<_, 'value>) : Row<'a, 'value> list =
        Generic.View.responseAsRows response

    /// Returns the response as a list of `SingleResponse`.
    /// Will return a list with a single element for a single response query.
    let responseAsSingleResponses (response: Response<_, _>) : SingleResponse<_, _> list =
        Generic.View.responseAsSingleResponses response
    