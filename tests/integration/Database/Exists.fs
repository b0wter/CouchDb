namespace b0wter.CouchDb.Tests.Integration.Database

module Exists =
    
    open FsUnit.Xunit
    open Xunit
    open b0wter.CouchDb.Lib
    open b0wter.CouchDb.Tests.Integration
    
    type Tests() =
        inherit Utilities.CleanDatabaseTests()

        [<Fact>]
        member this.``Querying for an existing database returns Exists-result`` () =
            this.FailIfNotInitialized()
            async {
               let dbNames = [ "exists-test-db-1"; "exists-test-db-2" ]
               match! Initialization.createDatabases dbNames with
               | true ->
                   let! result = Database.Exists.query Initialization.defaultDbProperties dbNames.Head
                   match result with
                   | Database.Exists.Result.Exists -> true
                   | Database.Exists.Result.DoesNotExist -> false
                   | Database.Exists.Result.RequestError _ -> false
                   |> should equal true
               | false ->
                   return failwith "The database creation (preparation) failed."
            }
            
        [<Fact>]
        member this.``Querying for a non-existing database returns DoesNotExist-result`` () =
            this.FailIfNotInitialized()
            async {
               let dbName = "non-existing-db-1"
               let! result = Database.Exists.query Initialization.defaultDbProperties dbName
               match result with
               | Database.Exists.Result.Exists -> false
               | Database.Exists.Result.DoesNotExist -> true
               | Database.Exists.Result.RequestError _ -> false
               |> should equal true
            }

        [<Fact>]
        member this.``Querying with an empty database name returns RequestError-result`` () =
            this.FailIfNotInitialized()
            async {
               let dbName = ""
               let! result = Database.Exists.query Initialization.defaultDbProperties dbName
               match result with
               | Database.Exists.Result.Exists -> false
               | Database.Exists.Result.DoesNotExist -> false
               | Database.Exists.Result.RequestError _ -> true
               |> should equal true
            }
