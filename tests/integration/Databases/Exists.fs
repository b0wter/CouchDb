namespace b0wter.CouchDb.Tests.Integration.Databases

module Exists =
    
    open FsUnit.Xunit
    open Xunit
    open b0wter.CouchDb.Lib
    open b0wter.CouchDb.Tests.Integration
    open FsUnit.CustomMatchers
    
    type Tests() =
        inherit Utilities.CleanDatabaseTests()

        [<Fact>]
        member this.``Querying for an existing database returns Exists-result`` () =
            async {
               let dbNames = [ "exists-test-db-1"; "exists-test-db-2" ]
               match! Initialization.createDatabases dbNames with
               | Ok _ ->
                   let! result = Databases.Exists.query Initialization.defaultDbProperties dbNames.Head
                   result |> should be (ofCase<@ Databases.Exists.Result.Exists @>)
               | Error e ->
                   return failwith e
            }
            
        [<Fact>]
        member this.``Querying for a non-existing database returns DoesNotExist-result`` () =
            async {
               let dbName = "non-existing-db-1"
               let! result = Databases.Exists.query Initialization.defaultDbProperties dbName
               result |> should be (ofCase <@ Databases.Exists.DoesNotExist @>)
            }

        [<Fact>]
        member this.``Querying with an empty database name returns RequestError-result`` () =
            async {
               let dbName = ""
               let! result = Databases.Exists.query Initialization.defaultDbProperties dbName
               result |> should be (ofCase <@ Databases.Exists.DbNameMissing @>)
            }
