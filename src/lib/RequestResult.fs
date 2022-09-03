namespace b0wter.CouchDb.Lib

module RequestResult =
    
    open System
    
    type Headers = Map<string, string>
    type StatusCode = int option

    /// <summary>
    /// Wraps a status code and a response body (byte array) as a record.
    /// </summary>
    type BinaryRequestResult = {
        StatusCode: StatusCode
        Content: byte array
        Headers: Headers
    }
    
    /// <summary>
    /// Wraps a status code and a response body (string) as a record.
    /// </summary>
    type StringRequestResult = {
        StatusCode: StatusCode
        Content: string
        Headers: Headers
    }
        
    /// Creates a RequestResult for a http response (which contains all information).
    let createTextWithHeaders (code: StatusCode, content: string, headers: Headers) =
        {
            StatusCode = code
            Content = content
            Headers = headers
        }
        
    /// Creates a RequestResult for a response without headers.
    let createText (code: StatusCode, content: string) = createTextWithHeaders (code, content, Map.empty)
    
    let createBinaryWithHeaders (code: StatusCode, content: byte [], headers: Headers) =
        {
            BinaryRequestResult.StatusCode = code
            Content = content
            Headers = headers
        }
    
    let createBinary (code: StatusCode, content: byte[]) = createBinaryWithHeaders (code, content, Map.empty)
    
    /// Creates a RequestResult whose content is specifically formatted to contain a json error.
    let createForJson (e: JsonDeserializationError.JsonDeserializationError, statusCode: StatusCode, headers) =
        let content = sprintf "JSON: %s%s%sREASON: %s%s" Environment.NewLine e.Json Environment.NewLine Environment.NewLine e.Reason
        createTextWithHeaders (statusCode, content, headers)
        
