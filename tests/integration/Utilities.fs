namespace b0wter.CouchDb.Tests.Integration

module Utilities =
    
    open Microsoft.FSharp.Quotations.Patterns
    open Microsoft.FSharp.Reflection
    
    [<AbstractClass>]
    type CleanDatabaseTests() =
        member this.IsAuthenticated = Initialization.authenticateCouchDbClient() |> Async.RunSynchronously 
        member this.IsInitialized = Initialization.deleteAllDatabases() |> Async.RunSynchronously 
        
        member this.FailIfNotInitialized () =
            if this.IsAuthenticated && this.IsInitialized then () else failwith "The initialization has failed."
            
    
    [<AbstractClass>]
    type PrefilledDatabaseTests() =
        inherit CleanDatabaseTests ()
        
        member this.RunWithDatabases dbNames (toRun: unit -> Async<unit>) =
            async {
                match! Initialization.createDatabases dbNames with
                | true -> return! toRun ()
                | false -> return failwith "The database preparation failed!"
            } |> Async.RunSynchronously
            
        member this.Run (toRun: unit -> Async<unit>) =
            async {
                return! toRun ()
            }

    let unionCases<'a> () =
        if FSharpType.IsUnion(typeof<'a>) then
            let cases = FSharpType.GetUnionCases typeof<'a>
            
            FsUnit.CustomMatchers.True
            0
        else
            1