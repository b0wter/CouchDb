namespace b0wter.CouchDb.Tests.Integration.Databases

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
                let! result = Databases.Create.query Initialization.defaultDbProperties dbName []
                result |> should be (ofCase <@ Databases.Create.Result.Created, Databases.Create.Result.Accepted @>)
            }

        [<Fact>]
        member this.``Creating a new database with an invalid name returns InvalidDbName-result`` () =
            async {
                let dbName = "00-this-[is]-{an}-inv@lid-name"
                let! result = Databases.Create.query Initialization.defaultDbProperties dbName []
                result |> should be (ofCase <@ Databases.Create.Result.InvalidDbName @>)
            }

        [<Fact>]
        member this.``Creating a new database with an existing name returns AlreadyExists-result`` () =
            async {
                let dbName = "test-db-1"
                let createCommand = fun () -> Databases.Create.query Initialization.defaultDbProperties dbName []
                do createCommand () |> Async.RunSynchronously |> ignore
                let! result = createCommand ()
                result |> should be (ofCase <@ Databases.Create.Result.AlreadyExists @>)
            }
            
        [<Fact>]
        member this.``Creating a new database with an empty name returns InvalidDbName-result`` () =
            async {
                let dbName = ""
                let! result = Databases.Create.query Initialization.defaultDbProperties dbName []
                result |> should be (ofCase <@ Databases.Create.Result.InvalidDbName @>)
            }

