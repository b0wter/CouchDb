namespace b0wter.CouchDb.Lib

module RequestResult =
    
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
        
    let createWithHeaders (code: StatusCode, content: string, headers: Headers) =
        {
            statusCode = code
            content = content
            headers = headers
        }
        
    let create (code: StatusCode, content: string) = createWithHeaders (code, content, Map.empty)
