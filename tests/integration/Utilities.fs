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
    type PrefilledDatabaseTests(dbNames: string list) =
        inherit CleanDatabaseTests ()
        do Initialization.createDatabases dbNames
           |> Async.RunSynchronously
           |> (fun x -> if x then printfn "Prefilled database is ok."
                        else failwith "Could not create the required databases.")
        
        /// <summary>
        /// Instatiate without creating databases.
        /// </summary>
        new() = PrefilledDatabaseTests([])
        
        /// <summary>
        /// Instantiates with a single database.
        /// </summary>
        new(dbName: string) = PrefilledDatabaseTests([dbName])
        
        /// <summary>
        /// Will run create queries for each supplied database name
        /// and the `toRun` afterwards.
        /// </summary>
        member this.RunWithDatabases dbNames (toRun: unit -> Async<unit>) =
            async {
                match! Initialization.createDatabases dbNames with
                | true -> return! toRun ()
                | false -> return failwith "The database preparation failed!"
            } |> Async.RunSynchronously
            
