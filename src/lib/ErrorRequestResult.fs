namespace b0wter.CouchDb.Lib

module ErrorRequestResult =

    open b0wter.FSharp.Operators
    open b0wter.FSharp
    open Utilities
    
    type T = {
        statusCode: RequestResult.StatusCode
        content: string
        headers: RequestResult.Headers
        case: string
    }
    
    let create (statusCode, content, headers, case) =
        { statusCode = statusCode; content = content; headers = headers; case = case }
    
    let fromRequestResult (r: RequestResult.T, case) =
        {
            statusCode = r.statusCode
            content = r.content
            headers = r.headers
            case = case
        }
        
    let fromRequestResultAndCase<'a>(r: RequestResult.T, case: 'a) =
        let caseName = case |> getUnionCaseName 
        fromRequestResult(r, caseName)