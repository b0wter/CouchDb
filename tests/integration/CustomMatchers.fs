namespace b0wter.CouchDb.Tests.Integration

module CustomMatchers =
    
    open b0wter.FSharp
    
    /// <summary>
    /// This is a custom matcher for use with Xunit.
    /// </summary>
    let ofCase (case: FSharp.Quotations.Expr) =
        // TODO: Remove this when this becomes a part of FsUnit.
        let expected = case |> Union.caseName |> defaultArg <| "<The given type is not a union case and the matcher won't work.>"
        let matcher = NHamcrest.Core.CustomMatcher(expected, fun x -> x |> b0wter.FSharp.Union.isCase case)
        matcher
        
