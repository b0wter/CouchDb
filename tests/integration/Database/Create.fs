namespace b0wter.CouchDb.Tests.Integration.Database
open System.Data

module Create =
    
    open FsUnit.Xunit
    open Xunit
    open b0wter.CouchDb.Lib
    open b0wter.CouchDb.Tests.Integration
    open CustomMatchers
    
    type Tests() =
        inherit Utilities.CleanDatabaseTests()
        
        [<Fact>]
        member this.``Creating a new database with a valid name returns Accepted-result`` () =
            async {
                let dbName = "test-db-1"
                let! result = Database.Create.query Initialization.defaultDbProperties dbName None None
                result |> should be (ofCase <@ Database.Create.Result.Created, Database.Create.Result.Accepted @>)
            }

        [<Fact>]
        member this.``Creating a new database with an invalid name returns InvalidDbName-result`` () =
            async {
                let dbName = "00-this-[is]-{an}-inv@lid-name"
                let! result = Database.Create.query Initialization.defaultDbProperties dbName None None
                result |> should be (ofCase <@ Database.Create.Result.InvalidDbName @>)
            }

        [<Fact>]
        member this.``Creating a new database with an existing name returns AlreadyExists-result`` () =
            async {
                let dbName = "test-db-1"
                let createCommand = fun () -> Database.Create.query Initialization.defaultDbProperties dbName None None
                do createCommand () |> Async.RunSynchronously |> ignore
                let! result = createCommand ()
                result |> should be (ofCase <@ Database.Create.Result.AlreadyExists @>)
            }
            
        [<Fact>]
        member this.``Creating a new database with an empty name returns InvalidDbName-result`` () =
            async {
                let dbName = ""
                let! result = Database.Create.query Initialization.defaultDbProperties dbName None None
                result |> should be (ofCase <@ Database.Create.Result.InvalidDbName @>)
            }

