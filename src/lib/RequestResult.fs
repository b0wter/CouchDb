namespace b0wter.CouchDb.Lib

module RequestResult =
    
    open Utilities
    open System
    
    type Headers = Map<string, string>
    type StatusCode = int option

    /// <summary>
    /// Wraps a status code and a response body (byte array) as a record.
    /// </summary>
    type TBinary = {
        StatusCode: StatusCode
        Content: byte array
        Headers: Headers
    }
    
    /// <summary>
    /// Wraps a status code and a response body (string) as a record.
    /// </summary>
    type TString = {
        StatusCode: StatusCode
        Content: string
        Headers: Headers
    }
        
    /// Creates a RequestResult for a http response (which contains all information).
    let createWithHeaders (code: StatusCode, content: string, headers: Headers) =
        {
            StatusCode = code
            Content = content
            Headers = headers
        }
        
    /// Creates a RequestResult for a response without headers.
    let create (code: StatusCode, content: string) = createWithHeaders (code, content, Map.empty)

    /// Creates a RequestResult whose content is specifically formatted to contain a json error.
    let createForJson (e: JsonDeserializationError.T, statusCode: StatusCode, headers) =
        let content = sprintf "JSON: %s%s%sREASON: %s%s" Environment.NewLine e.Json Environment.NewLine Environment.NewLine e.Reason
        createWithHeaders (statusCode, content, headers)
        
