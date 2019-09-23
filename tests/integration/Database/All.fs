namespace b0wter.CouchDb.Tests.Integration.Database

module All =
    
    open FsUnit.Xunit
    open Xunit
    open b0wter.CouchDb.Lib
    open b0wter.CouchDb.Tests.Integration
    
    type Tests() =
        inherit Utilities.CleanDatabaseTests()

        [<Fact>]
        member this.``Querying all databases on a prefilled database returns all databases`` () =
            this.FailIfNotInitialized ()
            async {
                let dbNames = [ "test-db-1"; "test-db-2"; "test-db-3" ]
                match! Initialization.createDatabases dbNames with
                | true ->
                    let! result = Database.All.query Initialization.defaultDbProperties
                    match result with
                    | Database.All.Result.Success s ->
                        if dbNames = s then true else false
                    | Database.All.Result.Failure _ ->
                        false
                    |> should equal true
                | false ->
                   return failwith "The database creation (preparation) failed."
            }

        [<Fact>]
        member this.``Querying empty server returns empty list`` () =
            this.FailIfNotInitialized ()
            async {
                let! result = Database.All.query Initialization.defaultDbProperties
                match result with
                | Database.All.Result.Success s ->
                    s |> should be Empty
                | Database.All.Result.Failure _ ->
                    failwith "The result is not empty when it should be."
            }