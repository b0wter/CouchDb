namespace b0wter.CouchDb.Lib

module ErrorRequestResult =

    open Utilities
    
    /// Wraps all http information of a failed CouchDb request. The case is no longer type-safe since
    /// it's stored as a string.
    type T = {
        StatusCode: RequestResult.StatusCode
        Content: string
        Headers: RequestResult.Headers
        Case: string
    }
    
    /// Creates an `ErrorRequestResult.T`.
    let create (statusCode, content, headers, case) =
        { StatusCode = statusCode; Content = content; Headers = headers; Case = case }
    
    /// Creates an `ErrorRequestResult.T` from a `RequestResult.T` and a case name.
    let fromRequestResult (r: RequestResult.T, case) =
        {
            StatusCode = r.StatusCode
            Content = r.Content
            Headers = r.Headers
            Case = case
        }
        
    /// Creates an `ErrorRequestResult.T` from a `RequestResult.T` and a case.
    /// Uses `getUnionCaseName` to retrieve the case name.
    let fromRequestResultAndCase<'a>(r: RequestResult.T, case: 'a) =
        let caseName = case |> getUnionCaseName 
        fromRequestResult(r, caseName)
        
    /// Returns a string in the format: "[$CASE] $CONTENT"
    let asString e =
        sprintf "[%s] %s" e.Case e.Content