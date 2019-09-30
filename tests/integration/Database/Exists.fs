namespace b0wter.CouchDb.Tests.Integration.Database

module Exists =
    
    open FsUnit.Xunit
    open Xunit
    open b0wter.CouchDb.Lib
    open b0wter.CouchDb.Tests.Integration
    open CustomMatchers
    
    type Tests() =
        inherit Utilities.CleanDatabaseTests()

        [<Fact>]
        member this.``Querying for an existing database returns Exists-result`` () =
            async {
               let dbNames = [ "exists-test-db-1"; "exists-test-db-2" ]
               match! Initialization.createDatabases dbNames with
               | true ->
                   let! result = Database.Exists.query Initialization.defaultDbProperties dbNames.Head
                   result |> should be (ofCase<@ Database.Exists.Result.Exists @>)
               | false ->
                   return failwith "The database creation (preparation) failed."
            }
            
        [<Fact>]
        member this.``Querying for a non-existing database returns DoesNotExist-result`` () =
            async {
               let dbName = "non-existing-db-1"
               let! result = Database.Exists.query Initialization.defaultDbProperties dbName
               result |> should be (ofCase <@ Database.Exists.DoesNotExist @>)
            }

        [<Fact>]
        member this.``Querying with an empty database name returns RequestError-result`` () =
            async {
               let dbName = ""
               let! result = Database.Exists.query Initialization.defaultDbProperties dbName
               result |> should be (ofCase <@ Database.Exists.RequestError @>)
            }
