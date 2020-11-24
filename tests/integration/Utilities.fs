namespace b0wter.CouchDb.Tests.Integration
open FsUnit
open b0wter.FSharp

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
        do if dbNames |> List.exists (System.String.IsNullOrWhiteSpace) then failwith "dbNames must not be empty!" else
           do Initialization.createDatabases dbNames
              |> Async.RunSynchronously
              |> (fun x -> match x with
                           | Ok _ -> ()
                           | Error e -> failwith e)
           
        /// Returns the database names that were supplied as constructor parameters.
        member this.DbNames = dbNames
        
        /// <summary>
        /// Instatiate without creating databases.
        /// </summary>
        new() = EmptyMultiDatabaseTests([])
        
        (*
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
        *)
            
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
        do if System.String.IsNullOrWhiteSpace(dbName) then failwith "If you supply a dbName it must not be empty." else
           do Initialization.createDatabases [ dbName ]
              |> Async.RunSynchronously
              |> (fun x -> match x with
                           | Ok _ -> ()
                           | Error e -> failwith e)
           
        /// Returns the database names that were supplied as constructor parameters.
        member this.DbName = dbName
        
        /// <summary>
        /// Instatiate without creating databases.
        /// </summary>
        new() = EmptySingleDatabaseTests()
        
        /// Will run a create query for the supplied database name and `toRun` afterwards.
        (*
        member this.RunWithDb dbName (toRun: unit -> Async<unit>) =
            async {
                match! Initialization.createDatabases [dbName] with
                | Ok _ -> return! toRun ()
                | Error e -> return failwith e
            } |> Async.RunSynchronously
        *)
            
    /// Removes all databases and creates the a database names 'dbName' and
    /// inserts all given 'documents'.
    [<AbstractClass>]
    type PrefilledSingleDatabaseTests(dbName: string, documents: obj list) =
        inherit EmptySingleDatabaseTests (dbName)
        let addDocument obj = Databases.AddDocument.query Initialization.defaultDbProperties dbName obj
                              |> Async.RunSynchronously
        let result = documents |> List.map addDocument
        do result |> List.iter (should be (ofCase <@ Databases.AddDocument.Result.Created @>))
        
        member this.DbName = dbName

        /// Retrieves all documents from the prefilled database.
        member this.AllDocuments<'a> () =
            async {
                let id = (Mango.Id System.Guid.Empty) 
                let selector = Mango.condition "_id" (Mango.Greater id)
                let expression = selector |> Mango.createExpressionWithLimit System.Int32.MaxValue
                let! result = Databases.Find.queryAsResult<'a> Initialization.defaultDbProperties dbName expression
                match result with
                | Ok r -> return r
                | Error e ->
                    return failwith (sprintf "Could not retrieve all documents from prefilled database because: %s"
                                         (e |> ErrorRequestResult.textAsString))
            }
            
        /// Assumes that the database contains a single item.
        /// Retrieves the document and throws if there is more than one document.
        member this.GetSingleDocument<'a> () =
            this.AllDocuments<'a> ()
            |> Async.map (
                             fun r ->
                                 if r.Docs.Length = 1 then r.Docs.[0]
                                 else if r.Docs.Length = 0 then failwith "Tried to retrieve the single document from the database but it is empty."
                                 else if r.Docs.Length > 1 then failwith "Tried to retrieve the single document from the database but it has more than one document."
                                 else failwith "Getting the single document failed for unknown reasons."
                         )
            
