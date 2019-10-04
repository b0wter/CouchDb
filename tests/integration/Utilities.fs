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
    
    [<AbstractClass>]
    type PrefilledDatabaseTests() =
        inherit CleanDatabaseTests()
    
    /// <summary>
    /// Cleans the database and prefills with databases prior
    /// to running a query against it.
    /// </summary>
    /// <remarks>
    /// Uses a special method to run the tests since not all tests
    /// may require the same databases.
    /// </remarks>
    [<AbstractClass>]
    type PrefilledMultiDatabaseTests(dbNames: string list) =
        inherit PrefilledDatabaseTests ()
        do Initialization.createDatabases dbNames
           |> Async.RunSynchronously
           |> (fun x -> if x then printfn "Prefilled database is ok."
                        else failwith "Could not create the required databases.")
           
        /// Returns the database names that were supplied as constructor parameters.
        member this.DbNames = dbNames
        
        /// <summary>
        /// Instatiate without creating databases.
        /// </summary>
        new() = PrefilledMultiDatabaseTests([])
        
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
            
    /// <summary>
    /// Cleans the database and prefills with databases prior
    /// to running a query against it.
    /// </summary>
    /// <remarks>
    /// Uses a special method to run the tests since not all tests
    /// may require the same databases.
    /// </remarks>
    [<AbstractClass>]
    type PrefilledSingleDatabaseTests(dbName: string) =
        inherit PrefilledDatabaseTests ()
        do Initialization.createDatabases [ dbName ]
           |> Async.RunSynchronously
           |> (fun x -> if x then printfn "Prefilled database is ok."
                        else failwith "Could not create the required databases.")
           
        /// Returns the database names that were supplied as constructor parameters.
        member this.DbName = dbName
        
        /// <summary>
        /// Instatiate without creating databases.
        /// </summary>
        new() = PrefilledSingleDatabaseTests()
        
        /// <summary>
        /// Will run create queries for each supplied database name
        /// and the `toRun` afterwards.
        /// </summary>
        member this.RunWithDatabase dbName (toRun: unit -> Async<unit>) =
            async {
                match! Initialization.createDatabases [dbName] with
                | true -> return! toRun ()
                | false -> return failwith "The database preparation failed!"
            } |> Async.RunSynchronously