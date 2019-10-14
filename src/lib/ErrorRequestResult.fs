namespace b0wter.CouchDb.Lib

module ErrorRequestResult =

    open Utilities
    
    /// Wraps all http information of a failed CouchDb request. The case is no longer type-safe since
    /// it's stored as a string.
    type T = {
        statusCode: RequestResult.StatusCode
        content: string
        headers: RequestResult.Headers
        case: string
    }
    
    /// Creates an `ErrorRequestResult.T`.
    let create (statusCode, content, headers, case) =
        { statusCode = statusCode; content = content; headers = headers; case = case }
    
    /// Creates an `ErrorRequestResult.T` from a `RequestResult.T` and a case name.
    let fromRequestResult (r: RequestResult.T, case) =
        {
            statusCode = r.statusCode
            content = r.content
            headers = r.headers
            case = case
        }
        
    /// Creates an `ErrorRequestResult.T` from a `RequestResult.T` and a case.
    /// Uses `getUnionCaseName` to retrieve the case name.
    let fromRequestResultAndCase<'a>(r: RequestResult.T, case: 'a) =
        let caseName = case |> getUnionCaseName 
        fromRequestResult(r, caseName)
        
    /// Returns a string in the format: "[$CASE] $CONTENT"
    let asString e =
        sprintf "[%s] %s" e.case e.content