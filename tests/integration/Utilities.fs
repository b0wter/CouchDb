namespace b0wter.CouchDb.Tests.Integration

module Utilities =
    
    /// <summary>
    /// Is used as a base class to contain tests.
    /// Since Xunit creates a new instance for each test the
    /// database will always be clean.
    /// </summary>
    [<AbstractClass>]
    type CleanDatabaseTests() =
        let authenticated = Initialization.authenticateCouchDbClient() |> Async.RunSynchronously
        let cleaned = Initialization.deleteAllDatabases() |> Async.RunSynchronously 
        do if authenticated && cleaned then
            ()
           else
            failwith <| sprintf "The database preparation failed (authenticated: %b; cleaned: %b)!" authenticated cleaned 
    
    /// <summary>
    /// Cleans the database and prefills with databases prior
    /// to running a query against it.
    /// </summary>
    /// <remarks>
    /// Uses a special method to run the tests since not all tests
    /// may require the same databases.
    /// </remarks>
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
            
