namespace b0wter.CouchDb.Tests.Integration

module CustomMatchers =
    
    open b0wter.FSharp
    
    let ofCase (case: FSharp.Quotations.Expr) =
        let expected = case |> Union.caseName |> defaultArg <| "<The given type is not a union case and the matcher won't work.>"
        let matcher = NHamcrest.Core.CustomMatcher(expected, fun x -> x |> b0wter.FSharp.Union.isCase case)
        matcher
        
