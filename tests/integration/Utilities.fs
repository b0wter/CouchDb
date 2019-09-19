namespace b0wter.CouchDb.Tests.Integration

module Utilities =
    
    [<AbstractClass>]
    type CleanDatabaseTests() =
        member this.IsAuthenticated = Initialization.authenticateCouchDbClient() |> Async.RunSynchronously 
        member this.IsInitialized = Initialization.deleteAllDatabases() |> Async.RunSynchronously 
        
        member this.FailIfNotInitialized () =
            if this.IsAuthenticated && this.IsInitialized then () else failwith "The initialization has failed."
        

