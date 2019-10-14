namespace b0wter.CouchDb.Lib

module Utilities =
    open Microsoft.FSharp.Reflection
    
    /// Retrieves the name of a union case.
    let getUnionCaseName (x:'a) = 
        match FSharpValue.GetUnionFields(x, typeof<'a>) with
        | case, _ -> case.Name  
    