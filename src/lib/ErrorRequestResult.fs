namespace b0wter.CouchDb.Lib

open System
open System.Text

module ErrorRequestResult =

    open Utilities
    
    /// Wraps all http information of a failed CouchDb request. The case is no longer type-safe since
    /// it's stored as a string.
    type StringErrorRequestResult = {
        StatusCode: RequestResult.StatusCode
        Content: string
        Headers: RequestResult.Headers
        Case: string
    }
    
    /// Creates an `ErrorRequestResult.TString`.
    let createString (statusCode, content, headers, case) =
        { StatusCode = statusCode; Content = content; Headers = headers; Case = case }
    
    /// Creates an `ErrorRequestResult.TString` from a `RequestResult.TString` and a case name.
    let fromRequestResult (r: RequestResult.StringRequestResult, case) =
        {
            StatusCode = r.StatusCode
            Content = r.Content
            Headers = r.Headers
            Case = case
        }
        
    /// Creates an `ErrorRequestResult.TString` from a `RequestResult.TString` and a case.
    /// Uses `getUnionCaseName` to retrieve the case name.
    let fromRequestResultAndCase<'a>(r: RequestResult.StringRequestResult, case: 'a) =
        let caseName = case |> getUnionCaseName 
        fromRequestResult(r, caseName)
        
    /// Returns a string in the format: "[$CASE] $CONTENT"
    let textAsString e =
        sprintf "[%s] %s" e.Case e.Content
        
    type BinaryErrorRequestResult = {
        StatusCode: RequestResult.StatusCode
        Content: byte []
        Headers: RequestResult.Headers
        Case: string
    }
    
    let createBinary (statusCode, content, headers, case) =
        { StatusCode = statusCode; Content = content; Headers = headers; Case = case }
    
    let fromBinaryRequestResult (r: RequestResult.BinaryRequestResult, case) =
        {
            StatusCode = r.StatusCode
            Content = r.Content
            Headers = r.Headers
            Case = case
        }
        
    let fromBinaryRequestResultAndCase<'a> (r: RequestResult.BinaryRequestResult, case: 'a) =
        let caseName = case |> getUnionCaseName
        fromBinaryRequestResult(r, caseName)
        
    let binaryAsString e =
        let binaryAsString = e.Content |> Encoding.UTF8.GetString
        let binaryAsHex = BitConverter.ToString(e.Content)
        sprintf "[%s] { as UTF8: %s } { as HEX: %s }" e.Case binaryAsString binaryAsHex
