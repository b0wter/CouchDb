namespace b0wter.CouchDb.Tests.Integration
open FsUnit

module Utilities =
    
    open CustomMatchers
    open b0wter.CouchDb.Lib
    open FsUnit.Xunit
    
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
    
    /// Base class for all tests that require the existence of one or more databases.
    [<AbstractClass>]
    type DatabaseTests() =
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
    type EmptyMultiDatabaseTests(dbNames: string list) =
        inherit DatabaseTests ()
        do Initialization.createDatabases dbNames
           |> Async.RunSynchronously
           |> (fun x -> match x with
                        | Ok _ -> printfn "Prefilled database is ok."
                        | Error e -> failwith e)
           
        /// Returns the database names that were supplied as constructor parameters.
        member this.DbNames = dbNames
        
        /// <summary>
        /// Instatiate without creating databases.
        /// </summary>
        new() = EmptyMultiDatabaseTests([])
        
        /// <summary>
        /// Will run create queries for each supplied database name
        /// and the `toRun` afterwards.
        /// </summary>
        member this.RunWithDatabases dbNames (toRun: unit -> Async<unit>) =
            async {
                match! Initialization.createDatabases dbNames with
                | Ok _ -> return! toRun ()
                | Error e -> return failwith e
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
    type EmptySingleDatabaseTests(dbName: string) =
        inherit DatabaseTests ()
        do Initialization.createDatabases [ dbName ]
           |> Async.RunSynchronously
           |> (fun x -> match x with
                        | Ok _ -> printfn "Prefilled database is ok."
                        | Error e -> failwith e)
           
        /// Returns the database names that were supplied as constructor parameters.
        member this.DbName = dbName
        
        /// <summary>
        /// Instatiate without creating databases.
        /// </summary>
        new() = EmptySingleDatabaseTests()
        
        /// <summary>
        /// Will run create queries for each supplied database name
        /// and the `toRun` afterwards.
        /// </summary>
        member this.RunWithDatabase dbName (toRun: unit -> Async<unit>) =
            async {
                match! Initialization.createDatabases [dbName] with
                | Ok _ -> return! toRun ()
                | Error e -> return failwith e
            } |> Async.RunSynchronously
            
    [<AbstractClass>]
    type PrefilledSingleDatabaseTests(dbName: string, documents: obj list) =
        inherit EmptySingleDatabaseTests (dbName)
        let addDocument obj = Databases.AddDocument.query Initialization.defaultDbProperties dbName obj |> Async.RunSynchronously
        let result = documents |> List.map addDocument
        do result |> List.iter (should be (ofCase <@ Databases.AddDocument.Result.Created @>))
        
        member this.DbName = dbName
