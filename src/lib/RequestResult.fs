namespace b0wter.CouchDb.Lib

module RequestResult =
    
    open Utilities
    open System
    
    type Headers = Map<string, string>
    type StatusCode = int option
    
    /// <summary>
    /// Wraps a status code and a response body (string) as a record.
    /// </summary>
    type T = {
        statusCode: StatusCode
        content: string
        headers: Headers
    }       
        
    /// Creates a RequestResult for a http response (which contains all information).
    let createWithHeaders (code: StatusCode, content: string, headers: Headers) =
        {
            statusCode = code
            content = content
            headers = headers
        }
        
    /// Creates a RequestResult for a response without headers.
    let create (code: StatusCode, content: string) = createWithHeaders (code, content, Map.empty)

    /// Creates a RequestResult whose content is specifically formatted to contain a json error.
    let createForJson (e: JsonDeserializationError.T, statusCode: StatusCode, headers) =
        let content = sprintf "JSON: %s%s%sREASON: %s%s" Environment.NewLine e.json Environment.NewLine Environment.NewLine e.reason
        createWithHeaders (statusCode, content, headers)
        
